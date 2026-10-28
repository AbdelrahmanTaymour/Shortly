using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.UpdateShortUrl;

internal sealed class UpdateShortUrlCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateShortUrlCommand>
{
    public async Task<Result> Handle(UpdateShortUrlCommand request, CancellationToken cancellationToken)
    {
        var shortUrl = await shortUrlRepository.GetByIdAsync(request.ShortUrlId, cancellationToken);
        if (shortUrl is null) return Result.Failure(ShortUrlErrors.NotFound);

        if (!ShortUrlAuthorization.CanManage(shortUrl, userContext))
            return Result.Failure(ShortUrlErrors.NotFound); // 404, not 403 — don't reveal existence to non-owners

        var urlResult = OriginalUrl.Create(request.OriginalUrl);
        if (urlResult.IsFailure) return urlResult;

        shortUrl.UpdateDestination(urlResult.Value);
        shortUrl.UpdateMetadata(request.Title, request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}