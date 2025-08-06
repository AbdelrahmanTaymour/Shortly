using FluentValidation;
using Shortly.Core.DTOs.UsersDTOs.Usage;

namespace Shortly.Core.Validators.Users;

public class UserUsageDtoValidator : AbstractValidator<UserUsageDto>
{
    public UserUsageDtoValidator()
    {
        RuleFor(x => x.MonthlyLinksCreated)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Monthly links created cannot be negative")
            .LessThanOrEqualTo(x => x.TotalLinksCreated)
            .WithMessage("Monthly links created cannot exceed total links created");

        RuleFor(x => x.MonthlyQrCodesCreated)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Monthly QR codes created cannot be negative")
            .LessThanOrEqualTo(x => x.TotalQrCodesCreated)
            .WithMessage("Monthly QR codes created cannot exceed total QR codes created");

        RuleFor(x => x.TotalLinksCreated)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Total links created cannot be negative");

        RuleFor(x => x.TotalQrCodesCreated)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Total QR codes created cannot be negative");

        RuleFor(x => x.MonthlyResetDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Monthly reset date cannot be in the future");
    }

}