using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.BulkDeleteShortUrls;

internal sealed class BulkDeleteShortUrlsCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IOrganizationAuthorizationService organizationAuthorization,
    IUnitOfWork unitOfWork) : ICommandHandler<BulkDeleteShortUrlsCommand, BulkOperationResponse>
{
    public async Task<Result<BulkOperationResponse>> Handle(BulkDeleteShortUrlsCommand request, CancellationToken cancellationToken)
    {
        var shortUrls = await shortUrlRepository.GetByIdsAsync(request.ShortUrlIds, cancellationToken);

        var succeeded = 0;
        foreach (var shortUrl in shortUrls)
        {
            if (!await ShortUrlAuthorization.CanManageAsync(shortUrl, userContext, organizationAuthorization, cancellationToken)) continue;
            shortUrlRepository.Remove(shortUrl);
            succeeded++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var totalRequested = request.ShortUrlIds.Count;
        var failed = totalRequested - succeeded;

        return Result.Success(new BulkOperationResponse(totalRequested, succeeded, failed));
    }
}