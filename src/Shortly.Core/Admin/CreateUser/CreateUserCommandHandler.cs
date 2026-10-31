using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.CreateUser;

internal sealed class AdminCreateUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IPasswordHasher passwordHasher)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.EmailOrUsernameExistsAsync(request.Email, request.Username, cancellationToken))
            return Result.Failure<Guid>(UserErrors.EmailOrUsernameNotUnique);

        var passwordHash = passwordHasher.Hash(request.Password);
        var utcNow = dateTimeProvider.UtcNow;

        var registerResult = User.Register(request.Email, request.Username, passwordHash, utcNow);
        if (registerResult.IsFailure) return Result.Failure<Guid>(registerResult.Error);

        var user = registerResult.Value;
        user.ConfirmEmail(utcNow);
        if (request.Role != UserRole.User) user.ChangeRole(request.Role, utcNow);

        userRepository.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}