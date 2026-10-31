using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Admin.BulkActivateUsers;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.BulkDeleteUsers;

public class BulkDeleteUsersCommandHandler(
    IUserRepository userRepository, 
    IUserContext userContext,
    IUnitOfWork unitOfWork, 
    IDateTimeProvider dateTimeProvider)
: ICommandHandler<BulkDeleteUsersCommand, BulkOperationResponse>
{
public async Task<Result<BulkOperationResponse>> Handle(BulkDeleteUsersCommand request, CancellationToken cancellationToken)
{
    var users = await userRepository.GetByIdsAsync(request.UserIds, cancellationToken); // add to IUserRepository
    var byId = users.ToDictionary(u => u.Id);
    var utcNow = dateTimeProvider.UtcNow;
    int success = 0, failure = 0;

    foreach (var id in request.UserIds)
    {
        if (!byId.TryGetValue(id, out var user) || user.SoftDelete(userContext.UserId, utcNow).IsFailure)
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