using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.BulkActivateShortUrls;

public sealed class BulkActivateShortUrlsCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<BulkActivateShortUrlsCommand, BulkOperationResponse>
{
    public async Task<Result<BulkOperationResponse>> Handle(BulkActivateShortUrlsCommand request, CancellationToken cancellationToken)
    {
        var shortUrls = await shortUrlRepository.GetByIdsAsync(request.ShortUrlIds, cancellationToken);

        var succeeded = 0;
        foreach (var shortUrl in shortUrls)
        {
            if (!ShortUrlAuthorization.CanManage(shortUrl, userContext)) continue;
            if (shortUrl.Activate().IsSuccess) succeeded++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BulkOperationResponse(request.ShortUrlIds.Count, succeeded, request.ShortUrlIds.Count - succeeded);
    }
}