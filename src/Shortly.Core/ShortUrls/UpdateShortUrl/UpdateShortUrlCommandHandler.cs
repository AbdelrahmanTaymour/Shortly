using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Exceptions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.UpdateShortUrl;

internal sealed class UpdateShortUrlCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IOrganizationAuthorizationService organizationAuthorization,
    ShortUrlOwnerResolver ownerResolver,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateShortUrlCommand>
{
    public async Task<Result> Handle(UpdateShortUrlCommand request, CancellationToken cancellationToken)
    {
        var shortUrl = await shortUrlRepository.GetByIdAsync(request.ShortUrlId, cancellationToken);
        if (shortUrl is null) return Result.Failure(ShortUrlErrors.NotFound);

        if (!await ShortUrlAuthorization.CanManageAsync(shortUrl, userContext, organizationAuthorization, cancellationToken))
            return Result.Failure(ShortUrlErrors.NotFound); // 404, not 403 — to not reveal existence

        var urlResult = OriginalUrl.Create(request.OriginalUrl);
        if (urlResult.IsFailure) return Result.Failure(urlResult.Error);

        var codeResult = ShortCode.Create(request.ShortCode);
        if (codeResult.IsFailure) return Result.Failure(codeResult.Error);

        var codeChanged = !string.Equals(shortUrl.ShortCode.Value, codeResult.Value.Value, StringComparison.Ordinal);
        if (codeChanged)
        {
            // Changing the code is a custom-short-code feature; editing other properties of a link is not.
            var limits = await ownerResolver.GetLimitsForLinkAsync(shortUrl, cancellationToken);
            if (!limits.CustomShortCode) return Result.Failure(ShortUrlErrors.CustomShortCodeNotAllowed);
        }

        if (codeChanged && await shortUrlRepository.ExistsByShortCodeAsync(codeResult.Value, cancellationToken))
            return Result.Failure(ShortUrlErrors.ShortCodeAlreadyExists);

        var utcNow = dateTimeProvider.UtcNow;

        var updateResult = shortUrl.Update(
            urlResult.Value,
            codeResult.Value,
            request.Title,
            request.Description,
            request.ClickLimit,
            request.IsActive,
            request.IsPrivate,
            shortUrl.IsPasswordProtected,
            shortUrl.PasswordHash,
            request.TrackingEnabled,
            request.ExpiresAt,
            utcNow);

        if (updateResult.IsFailure) return updateResult;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == "ux_short_urls_short_code")
        {
            return Result.Failure(ShortUrlErrors.ShortCodeAlreadyExists);
        }
    }
}