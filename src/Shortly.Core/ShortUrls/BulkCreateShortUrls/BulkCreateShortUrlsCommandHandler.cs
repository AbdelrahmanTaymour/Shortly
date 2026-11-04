using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Exceptions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.BulkCreateShortUrls;

internal sealed class BulkCreateShortUrlsCommandHandler(
    IShortUrlRepository shortUrlRepository,
    Abstractions.Authentication.IUserContext userContext,
    ShortUrlOwnerResolver ownerResolver,
    IUnitOfWork unitOfWork,
    IShortCodeGenerator shortCodeGenerator,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<BulkCreateShortUrlsCommand, BulkCreateShortUrlsResponse>
{
    public async Task<Result<BulkCreateShortUrlsResponse>> Handle(BulkCreateShortUrlsCommand request, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;

        // Same owner, organization-status and plan rules as single creation, plus the plan's bulk-creation entitlement.
        // The quota is checked below against the number of items that actually pass validation.
        var ownerResult = await ownerResolver.ResolveAsync(
            linksRequested: null, requireBulkCreation: true, utcNow, cancellationToken);
        if (ownerResult.IsFailure) return Result.Failure<BulkCreateShortUrlsResponse>(ownerResult.Error);
        var owner = ownerResult.Value;

        var candidates = request.Urls
            .Select(item => (item, code: shortCodeGenerator.Generate()))
            .ToList();

        var existing = await shortUrlRepository.GetExistingShortCodesAsync(
            [.. candidates.Select(c => c.code)], cancellationToken);

        // Track both database existing codes and codes generated within this current batch
        var usedInBatch = new HashSet<string>(existing, StringComparer.Ordinal);

        var validLinks = new List<ShortUrl>();
        var errors = new List<string>();
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

            var createResult = CreateLink(owner, item, originalUrlResult.Value, shortCodeResult.Value, utcNow);
            if (createResult.IsFailure)
            {
                failure++;
                errors.Add($"{item.OriginalUrl}: {createResult.Error.Message}");
                continue;
            }

            validLinks.Add(createResult.Value);
        }

        // All-or-nothing: if the valid items do not fit the remaining monthly quota, create none of them.
        if (validLinks.Count > owner.RemainingMonthlyLinks)
            return Result.Failure<BulkCreateShortUrlsResponse>(ShortUrlErrors.MonthlyLinkLimitReached);

        if (validLinks.Count > 0)
        {
            foreach (var link in validLinks) shortUrlRepository.Add(link);

            // The links and the usage counters are saved in one transaction; a concurrent usage update is
            // surfaced as ConcurrentUpdate, exactly as in single creation.
            owner.RecordLinksCreated(validLinks.Count, utcNow);

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    break;
                }
                catch (ConcurrencyException)
                {
                    return Result.Failure<BulkCreateShortUrlsResponse>(ShortUrlErrors.ConcurrentUpdate);
                }
                catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == ShortCodeUniqueIndex)
                {
                    // Another request took one of the generated codes between the check and the insert.
                    // The whole batch rolled back: give every link a fresh, re-checked code and insert again.
                    if (attempt >= MaxSaveAttempts)
                        return Result.Failure<BulkCreateShortUrlsResponse>(ShortUrlErrors.ShortCodeGenerationFailed);

                    await AssignFreshCodesAsync(validLinks, utcNow, cancellationToken);
                }
            }
        }

        // Map IDs after EF has flushed and populated database identity values
        var createdIds = validLinks.Select(u => u.Id).ToList();

        return new BulkCreateShortUrlsResponse(request.Urls.Count, validLinks.Count, failure, createdIds, errors);
    }

    private const int MaxSaveAttempts = 3;
    private const string ShortCodeUniqueIndex = "ux_short_urls_short_code";

    private async Task AssignFreshCodesAsync(List<ShortUrl> links, DateTime utcNow, CancellationToken cancellationToken)
    {
        // Unique within the batch, and checked against the database once. A code that is still taken after this (a new
        // race) is caught by the unique index and the insert is retried again.
        var used = new HashSet<string>(StringComparer.Ordinal);
        var candidates = new List<ShortCode>();
        foreach (var _ in links)
        {
            var code = shortCodeGenerator.Generate();
            while (!used.Add(code.Value)) code = shortCodeGenerator.Generate();
            candidates.Add(code);
        }

        var taken = await shortUrlRepository.GetExistingShortCodesAsync(candidates, cancellationToken);

        for (var i = 0; i < links.Count; i++)
        {
            var code = candidates[i];
            if (taken.Contains(code.Value))
            {
                do code = shortCodeGenerator.Generate();
                while (taken.Contains(code.Value) || !used.Add(code.Value));
            }

            links[i].AssignShortCode(code, utcNow);
        }
    }

    private Result<ShortUrl> CreateLink(
        ShortUrlOwner owner, BulkCreateShortUrlItem item, OriginalUrl originalUrl, ShortCode shortCode, DateTime utcNow)
    {
        if (owner.User is not null)
            return ShortUrl.CreateForUser(
                owner.User.Id, originalUrl, shortCode, utcNow, title: item.Title, description: item.Description);

        if (owner is { Organization: not null, OrganizationId: { } organizationId, MemberId: { } memberId })
            return ShortUrl.CreateForOrganization(
                organizationId, memberId, originalUrl, shortCode, utcNow, title: item.Title, description: item.Description);

        if (owner.AnonymousSessionId is { } anonymousSessionId)
            return ShortUrl.CreateForAnonymous(anonymousSessionId, userContext.IpAddress, originalUrl, shortCode, utcNow);

        return Result.Failure<ShortUrl>(ShortUrlErrors.InvalidOwnerType);
    }
}
