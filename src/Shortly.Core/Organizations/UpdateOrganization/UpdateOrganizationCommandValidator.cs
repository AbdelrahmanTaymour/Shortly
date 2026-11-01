using FluentValidation;

namespace Shortly.Core.Organizations.UpdateOrganization;

internal sealed class UpdateOrganizationCommandValidator
    : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.Website)
            .MaximumLength(2048)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.Website))
            .WithMessage("Website must be a valid URL.");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(2048)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.LogoUrl))
            .WithMessage("Logo URL must be a valid URL.");

        RuleFor(x => x.MemberLimit)
            .GreaterThan(0)
            .When(x => x.MemberLimit.HasValue);
    }

    private static bool BeValidUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}