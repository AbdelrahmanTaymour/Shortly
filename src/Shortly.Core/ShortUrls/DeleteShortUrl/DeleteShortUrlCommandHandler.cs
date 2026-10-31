using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.DeleteShortUrl;

internal sealed class DeleteShortUrlCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteShortUrlCommand>
{
    public async Task<Result> Handle(DeleteShortUrlCommand request, CancellationToken cancellationToken)
    {
        var shortUrl = await shortUrlRepository.GetByIdAsync(request.ShortUrlId, cancellationToken);
        if (shortUrl is null || !ShortUrlAuthorization.CanManage(shortUrl, userContext)) 
            return Result.Failure(ShortUrlErrors.NotFound);

        shortUrlRepository.Remove(shortUrl);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}