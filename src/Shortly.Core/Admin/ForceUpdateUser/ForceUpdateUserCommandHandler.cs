using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.ForceUpdateUser;

internal sealed class ForceUpdateUserCommandHandler(
    IUserContext userContext,
    IUserRepository userRepository,
    IUserActionTokenRepository tokenRepository,
    ITokenHasher tokenHasher,
    IEmailNotifier emailNotifier,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ForceUpdateUserCommand>
{
    public async Task<Result> Handle(ForceUpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) 
            return Result.Failure(UserErrors.NotFound);

        // Security Guard: Administrators cannot lock themselves out or demote their own privileges
        if (user.Id == userContext.UserId
            && (request.IsActive is false || (request.Role.HasValue && request.Role != user.Role)))
            return Result.Failure(UserErrors.CannotTargetSelf);

        var utcNow = dateTimeProvider.UtcNow;
        string? verificationToken = null;

        // Email Update & Re-verification Pipeline
        if (request.Email is not null && request.Email != user.Email)
        {
            var emailExists = await userRepository.EmailExistsAsync(request.Email, cancellationToken);
            if (emailExists) 
                return Result.Failure(UserErrors.EmailOrUsernameNotUnique);
            
            var changeResult = user.ChangeEmailUnverified(request.Email, utcNow);
            if (changeResult.IsFailure) 
                return changeResult;

            // Invalidate previous verification tokens and issue a fresh one
            await tokenRepository.InvalidateActiveTokensAsync(user.Id, enUserActionTokenType.EmailVerification, utcNow, cancellationToken);
            
            verificationToken = tokenHasher.GenerateToken();
            var issued = UserActionToken.IssueEmailVerification(user.Id, tokenHasher.Hash(verificationToken), utcNow);
            if (issued.IsFailure) 
                return Result.Failure(issued.Error);

            tokenRepository.Add(issued.Value);
        }

        // Username Update
        if (request.Username is not null && request.Username != user.Username)
        {
            var usernameExists = await userRepository.UsernameExistsAsync(request.Username, cancellationToken);
            if (usernameExists) 
                return Result.Failure(UserErrors.EmailOrUsernameNotUnique);
            
            var changeResult = user.ChangeUsername(request.Username, utcNow);
            if (changeResult.IsFailure) 
                return changeResult;
        }

        // System Role Change
        if (request.Role.HasValue && request.Role != user.Role)
        {
            var changeResult = user.ChangeRole(request.Role.Value, utcNow);
            if (changeResult.IsFailure) 
                return changeResult;
        }

        // Subscription Plan Tier Change
        if (request.SubscriptionPlanId.HasValue)
        {
            var changeResult = user.ChangeSubscriptionPlan(request.SubscriptionPlanId.Value, utcNow);
            if (changeResult.IsFailure) 
                return changeResult;
        }

        // Account Lifecycle Activation / Deactivation (Idempotent)
        if (request.IsActive is true)
        {
            var r = user.Activate(utcNow);
            if (r.IsFailure && r.Error != UserErrors.AlreadyActive) 
                return r;
        }
        else if (request.IsActive is false)
        {
            var r = user.Deactivate(utcNow);
            if (r.IsFailure && r.Error != UserErrors.AlreadyInactive) 
                return r;
        }

        // User Profile Patching
        var hasProfileChange = request.ProfileName is not null || request.ProfileBio is not null
            || request.ProfilePhoneNumber is not null || request.ProfileCompany is not null
            || request.ProfileLocation is not null || request.ProfileCountry is not null
            || request.ProfileTimeZone is not null;

        if (hasProfileChange)
        {
            user.Profile.Patch(
                request.ProfileName, request.ProfileBio, request.ProfilePhoneNumber, website: null,
                request.ProfileCompany, request.ProfileLocation, request.ProfileCountry, request.ProfileTimeZone, utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatch background notification after successful commit
        if (verificationToken is not null)
            emailNotifier.EnqueueSendEmailVerificationAsync(user.Email, user.Username, verificationToken);

        return Result.Success();
    }
}