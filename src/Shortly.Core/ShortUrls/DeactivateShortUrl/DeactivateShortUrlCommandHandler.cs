using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.DeactivateShortUrl;

internal sealed class DeactivateShortUrlCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IOrganizationAuthorizationService organizationAuthorization,
    IUnitOfWork unitOfWork) : ICommandHandler<DeactivateShortUrlCommand>
{
    public async Task<Result> Handle(DeactivateShortUrlCommand request, CancellationToken cancellationToken)
    {
        var shortUrl = await shortUrlRepository.GetByIdAsync(request.ShortUrlId, cancellationToken);
        if (shortUrl is null) return Result.Failure(ShortUrlErrors.NotFound);

        if (!await ShortUrlAuthorization.CanManageAsync(shortUrl, userContext, organizationAuthorization, cancellationToken))
            return Result.Failure(ShortUrlErrors.NotFound);

        var result = shortUrl.Deactivate();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}