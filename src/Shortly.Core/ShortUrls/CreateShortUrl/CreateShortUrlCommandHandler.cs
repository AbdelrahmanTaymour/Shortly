using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Cache;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Exceptions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.Core.ShortUrls.CreateShortUrl;

internal sealed class CreateShortUrlCommandHandler(
    IShortUrlRepository shortUrlRepository,
    ShortUrlOwnerResolver ownerResolver,
    IUserContext userContext,
    IShortCodeGenerator shortCodeGenerator,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateShortUrlCommand, string>
{
    private const int MaxShortCodeAttempts = 5;
    private const string ShortCodeUniqueIndex = "ux_short_urls_short_code";
    private const int UnlimitedClickLimit = -1;

    public async Task<Result<string>> Handle(CreateShortUrlCommand request, CancellationToken cancellationToken)
    {
        var urlResult = OriginalUrl.Create(request.OriginalUrl);
        if (urlResult.IsFailure) return Result.Failure<string>(urlResult.Error);

        var utcNow = dateTimeProvider.UtcNow;

        var ownerResult = await ownerResolver.ResolveAsync(
            linksRequested: 1, requireBulkCreation: false, utcNow, cancellationToken);
        if (ownerResult.IsFailure) return Result.Failure<string>(ownerResult.Error);

        // A custom short code is a plan feature of the link's owner (anonymous callers are Free).
        if (!string.IsNullOrWhiteSpace(request.CustomShortCode) && !ownerResult.Value.Limits.CustomShortCode)
            return Result.Failure<string>(ShortUrlErrors.CustomShortCodeNotAllowed);

        var shortCodeResult = await ResolveShortCodeAsync(request.CustomShortCode, cancellationToken);
        if (shortCodeResult.IsFailure) return Result.Failure<string>(shortCodeResult.Error);

        var shortUrlResult = CreateShortUrl(request, ownerResult.Value, urlResult.Value, shortCodeResult.Value, utcNow);
        if (shortUrlResult.IsFailure) return Result.Failure<string>(shortUrlResult.Error);

        var owner = ownerResult.Value;

        owner.RecordLinksCreated(1, utcNow);

        shortUrlRepository.Add(shortUrlResult.Value);

        var shortUrl = shortUrlResult.Value;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return shortUrl.ShortCode.Value;
            }
            catch (ConcurrencyException)
            {
                return Result.Failure<string>(ShortUrlErrors.ConcurrentUpdate);
            }
            catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == ShortCodeUniqueIndex)
            {
                // The code was free when it was checked but another request inserted it first.
                // A chosen code is simply taken; a generated one is replaced and the insert retried
                // (the usage increment above is still pending and is not applied twice).
                if (request.CustomShortCode is not null)
                    return Result.Failure<string>(ShortUrlErrors.ShortCodeAlreadyExists);

                if (attempt >= MaxShortCodeAttempts)
                    return Result.Failure<string>(ShortUrlErrors.ShortCodeGenerationFailed);

                shortUrl.AssignShortCode(shortCodeGenerator.Generate(), utcNow);
            }
        }
    }

    private Result<ShortUrl> CreateShortUrl(
        CreateShortUrlCommand request,
        ShortUrlOwner owner,
        OriginalUrl originalUrl,
        ShortCode shortCode,
        DateTime utcNow)
    {
        var clickLimit = request.ClickLimit ?? UnlimitedClickLimit;
        var trackingEnabled = request.TrackingEnabled ?? true;
        var isPrivate = request.IsPrivate ?? false;

        if (owner.User is not null)
        {
            return ShortUrl.CreateForUser(
                owner.User.Id,
                originalUrl,
                shortCode,
                utcNow,
                clickLimit,
                trackingEnabled,
                isPrivate,
                request.ExpiresAt,
                request.Title,
                request.Description);
        }

        if (owner is { Organization: not null, OrganizationId: { } organizationId, MemberId: { } memberId })
        {
            return ShortUrl.CreateForOrganization(
                organizationId,
                memberId,
                originalUrl,
                shortCode,
                utcNow,
                clickLimit,
                trackingEnabled,
                isPrivate,
                request.ExpiresAt,
                request.Title,
                request.Description);
        }

        if (owner.AnonymousSessionId is not null)
        {
            return ShortUrl.CreateForAnonymous(
                owner.AnonymousSessionId,
                userContext.IpAddress,
                originalUrl,
                shortCode,
                utcNow);
        }

        return Result.Failure<ShortUrl>(ShortUrlErrors.InvalidOwnerType);
    }

    private async Task<Result<ShortCode>> ResolveShortCodeAsync(
        string? customCode,
        CancellationToken cancellationToken)
    {
        if (customCode is not null)
        {
            var result = ShortCode.Create(customCode);
            if (result.IsFailure) return result;

            var exists = await shortUrlRepository.ExistsByShortCodeAsync(result.Value, cancellationToken);

            return exists
                ? Result.Failure<ShortCode>(ShortUrlErrors.ShortCodeAlreadyExists)
                : result;
        }

        for (var attempt = 0; attempt < MaxShortCodeAttempts; attempt++)
        {
            var candidate = shortCodeGenerator.Generate();

            var exists = await shortUrlRepository.ExistsByShortCodeAsync(candidate, cancellationToken);

            if (!exists) return candidate;
        }

        return Result.Failure<ShortCode>(ShortUrlErrors.ShortCodeGenerationFailed);
    }
}
