using FluentValidation;
using Microsoft.AspNetCore.Rewrite;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Extensions;

namespace Shortly.Core.Validators.ShortUrl;

public class CreateShortUrlRequestValidator : AbstractValidator<CreateShortUrlRequest>
{
    public CreateShortUrlRequestValidator()
    {
       RuleFor(x => x.OriginalUrl)
           .NotEmpty().WithMessage("Original URL is required.")
           .Must(ValidationExtensions.BeValidUrl).WithMessage("Original URL must be a valid URL.");

       RuleFor(x => x.CustomShortCode)
           .MaximumLength(15)
           .WithMessage("Custom short code cannot exceed 15 characters.")
           .Matches(@"^[a-zA-Z0-9_-]+$")
           .WithMessage("Custom short code can only contain letters, numbers, hyphens, and underscores.")
           .When(x => !string.IsNullOrEmpty(x.CustomShortCode));
       
       RuleFor(x => x.ClickLimit)
           .Must(x => x == -1 || x > 0)
           .WithMessage("Click limit must be -1 (unlimited) or a positive number.");

       RuleFor(x => x.Password)
           .NotEmpty()
           .WithMessage("Password is required when password protection is enabled.")
           .MinimumLength(6)
           .WithMessage("Password must be at least 6 characters long.")
           .When(x => x.IsPasswordProtected);

       RuleFor(x => x.Password)
           .Empty()
           .WithMessage("Password should not be provided when password protection is disabled.")
           .When(x => !x.IsPasswordProtected);
       
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
    }
}