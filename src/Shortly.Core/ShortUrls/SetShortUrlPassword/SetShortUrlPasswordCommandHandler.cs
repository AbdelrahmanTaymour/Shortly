using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.SetShortUrlPassword;

internal sealed class SetShortUrlPasswordCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IOrganizationAuthorizationService organizationAuthorization,
    ShortUrlOwnerResolver ownerResolver,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : ICommandHandler<SetShortUrlPasswordCommand>
{
    public async Task<Result> Handle(SetShortUrlPasswordCommand request, CancellationToken cancellationToken)
    {
        var shortUrl = await shortUrlRepository.GetByIdAsync(request.ShortUrlId, cancellationToken);
        if (shortUrl is null || !await ShortUrlAuthorization.CanManageAsync(shortUrl, userContext, organizationAuthorization, cancellationToken)) 
            return Result.Failure(ShortUrlErrors.NotFound);

        var limits = await ownerResolver.GetLimitsForLinkAsync(shortUrl, cancellationToken);
        if (!limits.LinkProtection) return Result.Failure(ShortUrlErrors.LinkProtectionNotAllowed);

        var result = shortUrl.ProtectWithPassword(passwordHasher.Hash(request.Password));
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}