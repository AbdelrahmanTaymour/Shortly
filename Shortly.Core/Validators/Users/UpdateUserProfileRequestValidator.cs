using FluentValidation;
using Shortly.Core.DTOs.UsersDTOs.Profile;

namespace Shortly.Core.Validators.Users;

public class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Bio)
            .MaximumLength(500)
            .WithMessage("Bio cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Bio));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Phone number must be in valid international format")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.ProfilePictureUrl)
            .Must(BeAValidUrl)
            .WithMessage("Profile picture URL must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.ProfilePictureUrl));

        RuleFor(x => x.Website)
            .Must(BeAValidUrl)
            .WithMessage("Website must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.Company)
            .MaximumLength(100)
            .WithMessage("Company name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Company));

        RuleFor(x => x.Location)
            .MaximumLength(100)
            .WithMessage("Location cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.Country)
            .Length(2, 3)
            .WithMessage("Country must be a 2 or 3 letter country code")
            .Matches(@"^[A-Z]{2,3}$")
            .WithMessage("Country must contain only uppercase letters")
            .When(x => !string.IsNullOrEmpty(x.Country));

        RuleFor(x => x.TimeZone)
            .Must(BeAValidTimeZone)
            .WithMessage("TimeZone must be a valid timezone identifier")
            .When(x => !string.IsNullOrEmpty(x.TimeZone));
    }
    
    private bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private bool BeAValidTimeZone(string? timeZone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone!);
            return true;
        }
        catch
        {
            return false;
        }
    }
}