using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.HardDeleteUser;

internal sealed class HardDeleteUserCommandHandler(
    IUserRepository userRepository,
    IShortUrlRepository shortUrlRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<HardDeleteUserCommand>
{
    public async Task<Result> Handle(HardDeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound);

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