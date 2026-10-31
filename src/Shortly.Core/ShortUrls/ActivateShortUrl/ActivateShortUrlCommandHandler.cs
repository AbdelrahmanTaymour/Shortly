using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.ActivateShortUrl;

public class ActivateShortUrlCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<ActivateShortUrlCommand>
{
    public async Task<Result> Handle(ActivateShortUrlCommand request, CancellationToken cancellationToken)
    {
        var shortUrl = await shortUrlRepository.GetByIdAsync(request.ShortUrlId, cancellationToken);
        if (shortUrl is null || 
            !ShortUrlAuthorization.CanManage(shortUrl, userContext)) 
            return Result.Failure(ShortUrlErrors.NotFound);

        var result = shortUrl.Activate();
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}