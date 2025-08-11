using FluentValidation;
using Shortly.Core.DTOs.UsersDTOs.Security;

namespace Shortly.Core.Validators.Users;

public class LockUserRequestValidation : AbstractValidator<LockUserRequest>
{
    public LockUserRequestValidation()
    {
        
        RuleFor(x => x.LockUntil)
            .NotNull()
            .WithMessage("LockUntil is required")
            .GreaterThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Monthly reset date cannot be in the past");
        
        
        RuleFor(x => x.Reason)
            .MaximumLength(100)
            .WithMessage("The reason cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Reason));
        
    }
}