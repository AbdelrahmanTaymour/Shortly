using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Enums;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.BulkCreateShortUrls;

internal sealed class BulkCreateShortUrlsCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    ShortCodeGenerator shortCodeGenerator,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<BulkCreateShortUrlsCommand, BulkCreateShortUrlsResponse>
{
    public async Task<Result<BulkCreateShortUrlsResponse>> Handle(BulkCreateShortUrlsCommand request, CancellationToken cancellationToken)
    {
        var candidates = request.Urls
            .Select(item => (item, code: shortCodeGenerator.Generate()))
            .ToList();

        var existing = await shortUrlRepository.GetExistingShortCodesAsync(
            [.. candidates.Select(c => c.code)], cancellationToken);

        // Track both database existing codes and codes generated within this current batch
        var usedInBatch = new HashSet<string>(existing, StringComparer.Ordinal);

        var pendingEntities = new List<ShortUrl>();
        var errors = new List<string>();
        var success = 0;
        var failure = 0;

        foreach (var (item, code) in candidates)
        {
            var finalCode = code;
            while (usedInBatch.Contains(finalCode.Value))
            {
                finalCode = shortCodeGenerator.Generate();
            }
            usedInBatch.Add(finalCode.Value);

            var originalUrlResult = OriginalUrl.Create(item.OriginalUrl);
            if (originalUrlResult.IsFailure)
            {
                failure++;
                errors.Add($"{item.OriginalUrl}: {originalUrlResult.Error.Message}");
                continue;
            }

            var shortCodeResult = ShortCode.Create(finalCode.Value);
            if (shortCodeResult.IsFailure)
            {
                failure++;
                errors.Add($"{item.OriginalUrl}: {shortCodeResult.Error.Message}");
                continue;
            }

            var utcNow = dateTimeProvider.UtcNow;

            var createResult = userContext.OwnerType switch
            {
                enShortUrlOwnerType.User => ShortUrl.CreateForUser(
                    userContext.UserId,
                    originalUrlResult.Value,
                    shortCodeResult.Value,
                    utcNow,
                    title: item.Title,
                    description: item.Description),

                enShortUrlOwnerType.Organization => userContext is { OrganizationId: { } organizationId, MemberId: { } memberId }
                    ? ShortUrl.CreateForOrganization(
                        organizationId,
                        memberId,
                        originalUrlResult.Value,
                        shortCodeResult.Value,
                        utcNow,
                        title: item.Title,
                        description: item.Description)
                    : Result.Failure<ShortUrl>(ShortUrlErrors.InvalidOwnerType),

                enShortUrlOwnerType.Anonymous => userContext is { AnonymousSessionId: { } anonymousSessionId, IpAddress: { } ipAddress }
                    ? ShortUrl.CreateForAnonymous(
                        anonymousSessionId,
                        ipAddress,
                        originalUrlResult.Value,
                        shortCodeResult.Value,
                        utcNow)
                    : Result.Failure<ShortUrl>(ShortUrlErrors.InvalidOwnerType),

                _ => Result.Failure<ShortUrl>(ShortUrlErrors.InvalidOwnerType)
            };

            if (createResult.IsFailure)
            {
                failure++;
                errors.Add($"{item.OriginalUrl}: {createResult.Error.Message}");
                continue;
            }

            shortUrlRepository.Add(createResult.Value);
            pendingEntities.Add(createResult.Value);
            success++;
        }

        if (success > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        // Map IDs after EF has flushed and populated database identity values
        var createdIds = pendingEntities.Select(u => u.Id).ToList();

        return new BulkCreateShortUrlsResponse(request.Urls.Count, success, failure, createdIds, errors);
    }
}