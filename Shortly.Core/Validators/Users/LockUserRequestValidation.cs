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
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Monthly reset date cannot be in the future");
        
        
        RuleFor(x => x.Reason)
            .MaximumLength(100)
            .WithMessage("The reason cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Reason));
        
    }
}