using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.BulkUpdateExpiration;

internal sealed class BulkUpdateExpirationCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<BulkUpdateExpirationCommand, BulkOperationResponse>
{
    public async Task<Result<BulkOperationResponse>> Handle(BulkUpdateExpirationCommand request, CancellationToken cancellationToken)
    {
        var urls = await shortUrlRepository.GetByIdsAsync(request.ShortUrlIds, cancellationToken);
        var byId = urls.ToDictionary(u => u.Id);

        var success = 0;
        var failure = 0;

        foreach (var id in request.ShortUrlIds)
        {
            if (!byId.TryGetValue(id, out var url) || !ShortUrlAuthorization.CanManage(url, userContext))
            {
                failure++;
                continue;
            }

            var result = url.SetExpiration(request.ExpiresAt, dateTimeProvider.UtcNow);
            if (result.IsFailure) { failure++; continue; }

            success++;
        }

        if (success > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BulkOperationResponse(request.ShortUrlIds.Count, success, failure);
    }
}