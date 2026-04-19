using FluentValidation;
using Shortly.Core.Extensions;
using Shortly.Core.ShortUrls.DTOs;

namespace Shortly.Core.ShortUrls.Validators;

public class UpdateShortUrlRequestValidator : AbstractValidator<UpdateShortUrlRequest>
{
    public UpdateShortUrlRequestValidator()
    {
        // URL Validation
        RuleFor(x => x.OriginalUrl)
            .Must(ValidationExtensions.BeValidUrl)
            .WithMessage("Original URL must be a valid URL.")
            .When(x => !string.IsNullOrEmpty(x.OriginalUrl));

        // Click Limit
        RuleFor(x => x.ClickLimit)
            .InclusiveBetween(-1, int.MaxValue)
            .NotEqual(0)
            .WithMessage("Click limit must be -1 or a positive number.")
            .When(x => x.ClickLimit.HasValue);

        // Password Logic:
        // We only validate length IF they are providing a new password.
        // We do NOT force a password here because it might already exist in the DB.
        RuleFor(x => x.Password)
            .MinimumLength(6)
            .WithMessage("New password must be at least 6 characters long.")
            .When(x => x.IsPasswordProtected == true && !string.IsNullOrEmpty(x.Password));

        // If they are disabling protection, they shouldn't be sending a password string.
        RuleFor(x => x.Password)
            .Empty()
            .WithMessage("Password should not be provided when disabling password protection.")
            .When(x => x.IsPasswordProtected == false);

        // Expiration
        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Expiration date must be in the future.")
            .When(x => x.ExpiresAt.HasValue);

        // Metadata
        RuleFor(x => x.Title).MaximumLength(50).When(x => x.Title != null);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);

        // Request Integrity
        RuleFor(x => x)
            .Must(HaveAtLeastOnePropertyToUpdate)
            .WithMessage("No changes were detected in the request.")
            .WithName("Request");
    }

    private static bool HaveAtLeastOnePropertyToUpdate(UpdateShortUrlRequest request)
    {
        return !string.IsNullOrEmpty(request.OriginalUrl) ||
               request.IsActive.HasValue ||
               request.TrackingEnabled.HasValue ||
               request.ClickLimit.HasValue ||
               request.IsPasswordProtected.HasValue ||
               !string.IsNullOrEmpty(request.Password) ||
               request.IsPrivate.HasValue ||
               request.ExpiresAt.HasValue ||
               request.Title != null ||
               request.Description != null;
    }
}