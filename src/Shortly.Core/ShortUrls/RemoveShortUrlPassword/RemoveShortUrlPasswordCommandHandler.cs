using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.RemoveShortUrlPassword;

public class RemoveShortUrlPasswordCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveShortUrlPasswordCommand>
{
    public async Task<Result> Handle(RemoveShortUrlPasswordCommand request, CancellationToken cancellationToken)
    {
        var shortUrl = await shortUrlRepository.GetByIdAsync(request.ShortUrlId, cancellationToken);
        if (shortUrl is null || !ShortUrlAuthorization.CanManage(shortUrl, userContext)) 
            return Result.Failure(ShortUrlErrors.NotFound);

        var result = shortUrl.RemovePassword();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}