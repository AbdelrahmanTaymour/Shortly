using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Admin.BulkActivateUsers;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.BulkDeactivateUsers;

internal sealed class BulkDeactivateUsersCommandHandler(
    IUserRepository userRepository, 
    IUnitOfWork unitOfWork, 
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<BulkDeactivateUsersCommand, BulkOperationResponse>
{
    public async Task<Result<BulkOperationResponse>> Handle(BulkDeactivateUsersCommand request, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetByIdsAsync(request.UserIds, cancellationToken); // add to IUserRepository
        var byId = users.ToDictionary(u => u.Id);
        var utcNow = dateTimeProvider.UtcNow;
        int success = 0, failure = 0;

        foreach (var id in request.UserIds)
        {
            if (!byId.TryGetValue(id, out var user) || user.Deactivate(utcNow).IsFailure)
            {
                failure++; 
                continue;
            }
            success++;
        }

        if (success > 0) await unitOfWork.SaveChangesAsync(cancellationToken);
        return new BulkOperationResponse(request.UserIds.Count, success, failure);
    }
}