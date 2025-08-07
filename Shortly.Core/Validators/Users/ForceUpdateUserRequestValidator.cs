using FluentValidation;
using Shortly.Core.DTOs.UsersDTOs.User;

namespace Shortly.Core.Validators.Users;

public class ForceUpdateUserRequestValidator : AbstractValidator<ForceUpdateUserRequest>
{
    public ForceUpdateUserRequestValidator()
    {
        RuleFor(x => x.User)
            .NotNull()
            .WithMessage("User data is required")
            .SetValidator(new UserDtoValidator());

        RuleFor(x => x.Profile)
            .NotNull()
            .WithMessage("Profile data is required")
            .SetValidator(new UserProfileDtoValidator());

        RuleFor(x => x.Security)
            .NotNull()
            .WithMessage("Security data is required")
            .SetValidator(new UserSecurityDtoValidator());

        RuleFor(x => x.Usage)
            .NotNull()
            .WithMessage("Usage data is required")
            .SetValidator(new UserUsageDtoValidator());
    }
}