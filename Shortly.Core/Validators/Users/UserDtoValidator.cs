using FluentValidation;
using Shortly.Core.DTOs.UsersDTOs.User;

namespace Shortly.Core.Validators.Users;

public class UserDtoValidator : AbstractValidator<UserDto>
{
    public UserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address")
            .MaximumLength(254)
            .WithMessage("Email cannot exceed 254 characters");

        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required")
            .Length(3, 50)
            .WithMessage("Username must be between 3 and 50 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Username can only contain letters, numbers, hyphens, and underscores");

        RuleFor(x => x.SubscriptionPlanId)
            .IsInEnum()
            .WithMessage("Invalid subscription plan");
        
        RuleFor(x => x.CreatedAt)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Created date cannot be in the future");

        RuleFor(x => x.UpdatedAt)
            .GreaterThanOrEqualTo(x => x.CreatedAt)
            .WithMessage("Updated date must be after or equal to created date");

        When(x => x.IsDeleted, () =>
        {
            RuleFor(x => x.DeletedAt)
                .NotNull()
                .WithMessage("Deleted date is required when user is marked as deleted")
                .GreaterThanOrEqualTo(x => x.CreatedAt)
                .WithMessage("Deleted date must be after created date")
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Deleted date cannot be in the future");

            RuleFor(x => x.DeletedBy)
                .NotNull()
                .WithMessage("DeletedBy is required when user is marked as deleted")
                .NotEqual(Guid.Empty)
                .WithMessage("DeletedBy must be a valid GUID");
        });

        When(x => !x.IsDeleted, () =>
        {
            RuleFor(x => x.DeletedAt)
                .Null()
                .WithMessage("Deleted date must be null when user is not deleted");

            RuleFor(x => x.DeletedBy)
                .Null()
                .WithMessage("DeletedBy must be null when user is not deleted");
        });
    }

}