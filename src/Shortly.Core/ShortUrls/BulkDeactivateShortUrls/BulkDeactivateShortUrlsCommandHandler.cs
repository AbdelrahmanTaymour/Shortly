using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.BulkDeactivateShortUrls;

internal sealed class BulkDeactivateShortUrlsCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IOrganizationAuthorizationService organizationAuthorization,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<BulkDeactivateShortUrlsCommand, BulkOperationResponse>
{
    public async Task<Result<BulkOperationResponse>> Handle(BulkDeactivateShortUrlsCommand request, CancellationToken cancellationToken)
    {
        var shortUrls = await shortUrlRepository.GetByIdsAsync(request.ShortUrlIds, cancellationToken);

        var utcNow = dateTimeProvider.UtcNow;
        var succeeded = 0;
        foreach (var shortUrl in shortUrls)
        {
            if (!await ShortUrlAuthorization.CanManageAsync(shortUrl, userContext, organizationAuthorization, cancellationToken)) continue;
            if (shortUrl.Deactivate(utcNow).IsSuccess) succeeded++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BulkOperationResponse(request.ShortUrlIds.Count, succeeded, request.ShortUrlIds.Count - succeeded);
    }
}