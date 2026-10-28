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
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateShortUrlCommand, long>
{
    public async Task<Result<long>> Handle(CreateShortUrlCommand request, CancellationToken cancellationToken)
    {
        var urlResult = OriginalUrl.Create(request.OriginalUrl);
        if (urlResult.IsFailure) return Result.Failure<long>(urlResult.Error);

        var shortCodeResult = await ResolveShortCodeAsync(request.CustomShortCode, cancellationToken);
        if (shortCodeResult.IsFailure) return Result.Failure<long>(shortCodeResult.Error);

        var utcNow = dateTimeProvider.UtcNow;

        var shortUrlResult = userContext.OwnerType switch
        {
            enShortUrlOwnerType.User => ShortUrl.CreateForUser(
                userContext.UserId, urlResult.Value, shortCodeResult.Value, utcNow,
                request.ClickLimit, request.TrackingEnabled, request.IsPrivate, request.ExpiresAt,
                request.Title, request.Description),

            enShortUrlOwnerType.Organization => ShortUrl.CreateForOrganization(
                userContext.OrganizationId!.Value, userContext.MemberId!.Value, urlResult.Value, shortCodeResult.Value, utcNow,
                request.ClickLimit, request.TrackingEnabled, request.IsPrivate, request.ExpiresAt,
                request.Title, request.Description),

            enShortUrlOwnerType.Anonymous => ShortUrl.CreateForAnonymous(
                userContext.AnonymousSessionId!, userContext.IpAddress, urlResult.Value, shortCodeResult.Value, utcNow,
                request.ClickLimit, request.ExpiresAt),

            _ => Result.Failure<ShortUrl>(ShortUrlErrors.OriginalUrlInvalid)
        };

        if (shortUrlResult.IsFailure) return Result.Failure<long>(shortUrlResult.Error);

        shortUrlRepository.Add(shortUrlResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return shortUrlResult.Value.Id;
    }

    private async Task<Result<ShortCode>> ResolveShortCodeAsync(string? customCode, CancellationToken cancellationToken)
    {
        if (customCode is not null)
        {
            var customResult = ShortCode.Create(customCode);
            if (customResult.IsFailure) return customResult;

            return await shortUrlRepository.ExistsByShortCodeAsync(customCode, cancellationToken)
                ? Result.Failure<ShortCode>(ShortUrlErrors.ShortCodeInvalidCharacters)
                : customResult;
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = shortCodeGenerator.Generate();
            if (!await shortUrlRepository.ExistsByShortCodeAsync(candidate.Value, cancellationToken))
                return candidate;
        }

        return Result.Failure<ShortCode>(ShortUrlErrors.ShortCodeInvalidLength);
    }
}