using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.ForceUpdateUser;

internal sealed class ForceUpdateUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ForceUpdateUserCommand>
{
    public async Task<Result> Handle(ForceUpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound);

        var utcNow = dateTimeProvider.UtcNow;

        if (request.Email is not null && request.Email != user.Email)
        {
            var emailExists = await userRepository.EmailExistsAsync(request.Email, cancellationToken);
            if (emailExists) return Result.Failure(UserErrors.EmailOrUsernameNotUnique);
            
            var changeResult = user.ChangeEmail(request.Email, utcNow);
            
            if (changeResult.IsFailure) 
                return changeResult;
        }

        if (request.Username is not null && request.Username != user.Username)
        {
            var usernameExists = await userRepository.UsernameExistsAsync(request.Username, cancellationToken);
            if(usernameExists) return Result.Failure(UserErrors.EmailOrUsernameNotUnique);
            
            var changeResult = user.ChangeUsername(request.Username, utcNow);
            
            if (changeResult.IsFailure) return changeResult;
        }

        if (request.Role.HasValue && request.Role != user.Role)
        {
            var changeResult = user.ChangeRole(request.Role.Value, utcNow);
            if (changeResult.IsFailure) return changeResult;
        }

        if (request.SubscriptionPlanId.HasValue)
        {
            var changeResult = user.ChangeSubscriptionPlan(request.SubscriptionPlanId.Value, utcNow);
            if (changeResult.IsFailure) return changeResult;
        }

        if (request.IsActive is true)
        {
            var r = user.Activate(utcNow);
            if (r.IsFailure && r.Error != UserErrors.AlreadyActive) return r;
        }
        else if (request.IsActive is false)
        {
            var r = user.Deactivate(utcNow);
            if (r.IsFailure && r.Error != UserErrors.AlreadyInactive) return r;
        }

        var hasProfileChange = request.ProfileName is not null || request.ProfileBio is not null
            || request.ProfilePhoneNumber is not null || request.ProfileCompany is not null
            || request.ProfileLocation is not null || request.ProfileCountry is not null
            || request.ProfileTimeZone is not null;

        if (hasProfileChange)
        {
            user.Profile.Update(
                request.ProfileName ?? user.Profile.Name,
                request.ProfileBio ?? user.Profile.Bio,
                request.ProfilePhoneNumber ?? user.Profile.PhoneNumber,
                user.Profile.Website,
                request.ProfileCompany ?? user.Profile.Company,
                request.ProfileLocation ?? user.Profile.Location,
                request.ProfileCountry ?? user.Profile.Country,
                request.ProfileTimeZone ?? user.Profile.TimeZone,
                utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}