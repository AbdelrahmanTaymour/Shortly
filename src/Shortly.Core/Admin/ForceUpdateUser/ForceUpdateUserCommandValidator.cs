using FluentValidation;

namespace Shortly.Core.Admin.ForceUpdateUser;

internal sealed class ForceUpdateUserCommandValidator : AbstractValidator<ForceUpdateUserCommand>
{
    public ForceUpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(255)
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Username)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.Username));

        RuleFor(x => x.Role)
            .IsInEnum()
            .When(x => x.Role.HasValue);

        RuleFor(x => x.ProfileName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.ProfileName));

        RuleFor(x => x.ProfileBio)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.ProfileBio));

        RuleFor(x => x.ProfilePhoneNumber)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.ProfilePhoneNumber));

        RuleFor(x => x.ProfileCompany)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.ProfileCompany));

        RuleFor(x => x.ProfileLocation)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.ProfileLocation));

        RuleFor(x => x.ProfileCountry)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.ProfileCountry));

        RuleFor(x => x.ProfileTimeZone)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.ProfileTimeZone));
    }
}