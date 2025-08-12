using FluentValidation;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Extensions;

namespace Shortly.Core.Validators.ShortUrl;

public class UpdateShortUrlRequestValidator : AbstractValidator<UpdateShortUrlRequest>
{
    public UpdateShortUrlRequestValidator()
    {
        RuleFor(x => x.OriginalUrl)
            .Must(ValidationExtensions.BeValidUrl)
            .WithMessage("Original URL must be a valid URL.")
            .When(x => !string.IsNullOrEmpty(x.OriginalUrl));
        
        RuleFor(x => x.ClickLimit)
            .Must(x => x == -1 || x > 0)
            .WithMessage("Click limit must be -1 (unlimited) or a positive number.")
            .When(x => x.ClickLimit.HasValue);
        
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required when password protection is enabled.")
            .MinimumLength(6)
            .WithMessage("Password must be at least 6 characters long.")
            .When(x => x.IsPasswordProtected == true && !string.IsNullOrEmpty(x.Password));

        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.Password))
            .WithMessage("Password is required when enabling password protection.")
            .WithName("Password")
            .When(x => x.IsPasswordProtected == true);
        
        RuleFor(x => x.Password)
            .Empty()
            .WithMessage("Password should not be provided when disabling password protection.")
            .When(x => x.IsPasswordProtected == false);
        
        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Expiration date must be in the future.")
            .When(x => x.ExpiresAt.HasValue);
        
        RuleFor(x => x.Title)
            .MaximumLength(50)
            .WithMessage("Title cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x)
            .Must(HaveAtLeastOnePropertyToUpdate)
            .WithMessage("At least one property must be updated.")
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
               !string.IsNullOrEmpty(request.Title) ||
               !string.IsNullOrEmpty(request.Description);
    }

}