using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Users.UpdateProfile;

internal sealed class UpdateProfileCommandHandler(
    IUserContext userContext, 
    IUserRepository userRepository, 
    IUnitOfWork unitOfWork, 
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateProfileCommand>
{
    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken);
        if (user is null) 
            return Result.Failure(UserErrors.NotFound);

        user.Profile.Update(request.Name, request.Bio, request.PhoneNumber, request.Website,
            request.Company, request.Location, request.Country, request.TimeZone, dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}