using FluentValidation;
using Shortly.Core.DTOs.UserDTOs;

namespace Shortly.Core.Validators;

public class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Length(2, 100).WithMessage("Name must be between 2 and 100 characters.");

        RuleFor(x => x.TimeZone)
            .MaximumLength(50).WithMessage("TimeZone cannot exceed 50 characters.");

        RuleFor(x => x.ProfilePictureUrl)
            .Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Profile picture URL must be a valid URL.");
    }
}