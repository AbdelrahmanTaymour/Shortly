using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Enums;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.CreateShortUrl;

internal sealed class CreateShortUrlCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    ShortCodeGenerator shortCodeGenerator,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateShortUrlCommand, string>
{
    public async Task<Result<string>> Handle(CreateShortUrlCommand request, CancellationToken cancellationToken)
    {
        var urlResult = OriginalUrl.Create(request.OriginalUrl);
        if (urlResult.IsFailure) return Result.Failure<string>(urlResult.Error);

        var shortCodeResult = await ResolveShortCodeAsync(request.CustomShortCode, cancellationToken);
        if (shortCodeResult.IsFailure) return Result.Failure<string>(shortCodeResult.Error);

        var utcNow = dateTimeProvider.UtcNow;

        var shortUrlResult = userContext.OwnerType switch
        {
            enShortUrlOwnerType.User => ShortUrl.CreateForUser(
                userContext.UserId, urlResult.Value, shortCodeResult.Value, utcNow,
                request.ClickLimit ?? -1, request.TrackingEnabled ?? true, request.IsPrivate ?? false,
                request.ExpiresAt,
                request.Title, request.Description),

            enShortUrlOwnerType.Organization => ShortUrl.CreateForOrganization(
                userContext.OrganizationId!.Value, userContext.MemberId!.Value, urlResult.Value, shortCodeResult.Value,
                utcNow,
                request.ClickLimit ?? -1, request.TrackingEnabled ?? true, request.IsPrivate ?? false,
                request.ExpiresAt,
                request.Title, request.Description),

            enShortUrlOwnerType.Anonymous => ShortUrl.CreateForAnonymous(
                userContext.AnonymousSessionId!, userContext.IpAddress, urlResult.Value, shortCodeResult.Value, utcNow),

            _ => Result.Failure<ShortUrl>(ShortUrlErrors.OriginalUrlInvalid)
        };

        if (shortUrlResult.IsFailure) return Result.Failure<string>(shortUrlResult.Error);

        shortUrlRepository.Add(shortUrlResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return shortUrlResult.Value.ShortCode.Value;
    }

    private async Task<Result<ShortCode>> ResolveShortCodeAsync(string? customCode, CancellationToken cancellationToken)
    {
        if (customCode is not null)
        {
            var customResult = ShortCode.Create(customCode);
            if (customResult.IsFailure) return customResult;

            var exists = await shortUrlRepository.ExistsByShortCodeAsync(customResult.Value, cancellationToken);
            return exists ? Result.Failure<ShortCode>(ShortUrlErrors.ShortCodeAlreadyExists) : customResult;
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = shortCodeGenerator.Generate();
            if (!await shortUrlRepository.ExistsByShortCodeAsync(candidate, cancellationToken))
                return candidate;
        }

        return Result.Failure<ShortCode>(ShortUrlErrors.ShortCodeInvalidLength);
    }
}