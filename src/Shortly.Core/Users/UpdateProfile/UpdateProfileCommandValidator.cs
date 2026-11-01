using FluentValidation;

namespace Shortly.Core.Users.UpdateProfile;

internal sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Name).MaximumLength(100);
        RuleFor(x => x.Bio).MaximumLength(500);
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.Website).MaximumLength(500);
        RuleFor(x => x.Company).MaximumLength(100);
        RuleFor(x => x.Location).MaximumLength(100);
        RuleFor(x => x.Country).MaximumLength(50);
        RuleFor(x => x.TimeZone).MaximumLength(50);
    }
}