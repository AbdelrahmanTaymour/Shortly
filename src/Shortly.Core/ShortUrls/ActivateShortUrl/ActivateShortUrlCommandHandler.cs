using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.ActivateShortUrl;

internal sealed class ActivateShortUrlCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IOrganizationAuthorizationService organizationAuthorization,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ActivateShortUrlCommand>
{
    public async Task<Result> Handle(ActivateShortUrlCommand request, CancellationToken cancellationToken)
    {
        var shortUrl = await shortUrlRepository.GetByIdAsync(request.ShortUrlId, cancellationToken);
        if (shortUrl is null || 
            !await ShortUrlAuthorization.CanManageAsync(shortUrl, userContext, organizationAuthorization, cancellationToken)) 
            return Result.Failure(ShortUrlErrors.NotFound);

        var result = shortUrl.Activate(dateTimeProvider.UtcNow);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}