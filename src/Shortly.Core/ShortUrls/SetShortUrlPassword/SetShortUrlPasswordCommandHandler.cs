using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.SetShortUrlPassword;

internal sealed class SetShortUrlPasswordCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : ICommandHandler<SetShortUrlPasswordCommand>
{
    public async Task<Result> Handle(SetShortUrlPasswordCommand request, CancellationToken cancellationToken)
    {
        var shortUrl = await shortUrlRepository.GetByIdAsync(request.ShortUrlId, cancellationToken);
        if (shortUrl is null || !ShortUrlAuthorization.CanManage(shortUrl, userContext)) 
            return Result.Failure(ShortUrlErrors.NotFound);

        var result = shortUrl.ProtectWithPassword(passwordHasher.Hash(request.Password));
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}