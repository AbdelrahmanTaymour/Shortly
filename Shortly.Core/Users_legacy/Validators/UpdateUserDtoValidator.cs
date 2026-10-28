using FluentValidation;
using Shortly.Core.Users_legacy.DTOs.User;

namespace Shortly.Core.Users_legacy.Validators;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserDtoValidator()
    {
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

        RuleFor(x => x.Permissions)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Permissions cannot be negative");
    }
}