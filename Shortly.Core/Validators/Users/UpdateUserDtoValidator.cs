using FluentValidation;
using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Extensions;

namespace Shortly.Core.Validators.Users;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        /*RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Length(2, 100).WithMessage("Name must be between 2 and 100 characters.")
            .Must(name => name.IsValidName()).WithMessage("Name can only contain letters, spaces, hyphens, apostrophes, and periods.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, 50).WithMessage("Username must be between 3 and 50 characters.")
            .Must(username => username.IsValidUsername()).WithMessage("Username can only contain letters, numbers, underscores, hyphens, and periods.")
            .Must(username => !username.StartsWith('.') && !username.EndsWith('.')).WithMessage("Username cannot start or end with a period.");

        RuleFor(x => x.SubscriptionPlan)
            .IsInEnum().WithMessage("Invalid subscription plan value.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role value.");

        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("IsActive status must be specified.");

        RuleFor(x => x.IsEmailConfirmed)
            .NotNull().WithMessage("IsEmailConfirmed status must be specified.");

        RuleFor(x => x.ProfilePictureUrl)
            .Must(url => url.IsValidImageUrl())
            .When(x => !string.IsNullOrWhiteSpace(x.ProfilePictureUrl))
            .WithMessage("Profile picture URL must be a valid image URL (http/https with jpg, jpeg, png, gif, webp, bmp, or svg extension).");

        RuleFor(x => x.TimeZone)
            .Must(tz => tz.IsValidTimeZone())
            .When(x => !string.IsNullOrWhiteSpace(x.TimeZone))
            .WithMessage("Invalid time zone identifier.");

        RuleFor(x => x.MonthlyLinksCreated)
            .GreaterThanOrEqualTo(0).WithMessage("Monthly links created cannot be negative.");

        RuleFor(x => x.TotalLinksCreated)
            .GreaterThanOrEqualTo(0).WithMessage("Total links created cannot be negative.");

        RuleFor(x => x.MonthlyResetDate)
            .NotEmpty().WithMessage("Monthly reset date is required.");

        RuleFor(x => x.FailedLoginAttempts)
            .GreaterThanOrEqualTo(0).WithMessage("Failed login attempts cannot be negative.");

        RuleFor(x => x.LockedUntil)
            .Must(date => date.IsInFuture())
            .When(x => x.LockedUntil.HasValue)
            .WithMessage("Lock expiration date must be in the future.");*/
        
        throw new NotImplementedException();

    }
}