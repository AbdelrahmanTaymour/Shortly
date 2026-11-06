using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.HardDeleteUser;

internal sealed class HardDeleteUserCommandHandler(
    IUserContext userContext,
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    IShortUrlRepository shortUrlRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<HardDeleteUserCommand>
{
    public async Task<Result> Handle(HardDeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == userContext.UserId) return Result.Failure(UserErrors.CannotTargetSelf);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound);

        if (await organizationRepository.CountOwnedByAsync(user.Id, cancellationToken) > 0)
            return Result.Failure(UserErrors.OwnsOrganizations);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            if (request.DeleteOwnedShortUrls)
            {
                await shortUrlRepository.DeleteAllForUserAsync(request.UserId, cancellationToken);
            }
            else
            {
                await shortUrlRepository.DetachAllFromUserAsync(request.UserId, cancellationToken);
            }

            userRepository.Remove(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            
            return Result.Success();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}