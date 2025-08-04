using System.Text.RegularExpressions;
using FluentValidation;
using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Extensions;

namespace Shortly.Core.Validators.Users;

public partial class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    // Pre-compiled regex patterns for better performance
    private static readonly Regex NamePattern = MyNameRegex();
    private static readonly Regex UsernamePattern = MyUsernameRegex();

    // Cache for valid image extensions
    private static readonly HashSet<string> ValidImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg"
    };

    // Cache for time zones
    private static readonly Lazy<HashSet<string>> ValidTimeZones = new(() =>
        TimeZoneInfo.GetSystemTimeZones().Select(tz => tz.Id).ToHashSet(StringComparer.OrdinalIgnoreCase));

    
    public CreateUserRequestValidator()
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

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .Length(8, 128).WithMessage("Password must be between 8 and 128 characters.")
            .Must(password => password.HasPasswordComplexity()).WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");

        RuleFor(x => x.SubscriptionPlan)
            .IsInEnum()
            .When(x => x.SubscriptionPlan.HasValue)
            .WithMessage("Invalid subscription plan value.");

        RuleFor(x => x.Role)
            .IsInEnum()
            .When(x => x.Role.HasValue)
            .WithMessage("Invalid role value.");

        RuleFor(x => x.ProfilePictureUrl)
            .Must(url => url.IsValidImageUrl())
            .When(x => !string.IsNullOrWhiteSpace(x.ProfilePictureUrl))
            .WithMessage("Profile picture URL must be a valid image URL (http/https with jpg, jpeg, png, gif, webp, bmp, or svg extension).");

        RuleFor(x => x.TimeZone)
            .Must(tz => tz.IsValidTimeZone())
            .When(x => !string.IsNullOrWhiteSpace(x.TimeZone))
            .WithMessage("Invalid time zone identifier.");

        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("IsActive status must be specified.");
            */

        throw new NotImplementedException();
    }
    
    // Optimized password complexity check - single pass through password
    private static bool HasPasswordComplexity(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        bool hasUpper = false, hasLower = false, hasDigit = false, hasSpecial = false;

        foreach (char c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else hasSpecial = true;

            // Early exit if all conditions are met
            if (hasUpper && hasLower && hasDigit && hasSpecial)
                return true;
        }

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    // Optimized URL and image extension validation
    private static bool BeAValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        // Fast URI validation
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        // Fast extension check using ReadOnlySpan to avoid string allocations
        ReadOnlySpan<char> urlSpan = url.AsSpan();
        int lastDotIndex = urlSpan.LastIndexOf('.');
        
        if (lastDotIndex == -1 || lastDotIndex == urlSpan.Length - 1)
            return false;

        ReadOnlySpan<char> extension = urlSpan.Slice(lastDotIndex);
        return ValidImageExtensions.Contains(extension.ToString());
    }

    // Optimized time zone validation using cached HashSet
    private static bool BeAValidTimeZone(string? timeZone)
    {
        return string.IsNullOrWhiteSpace(timeZone) || ValidTimeZones.Value.Contains(timeZone);
    }
    
    

    [GeneratedRegex(@"^[a-zA-Z\s\-'\.]+$", RegexOptions.Compiled)]
    private static partial Regex MyNameRegex();
    [GeneratedRegex(@"^[a-zA-Z0-9_\-\.]+$", RegexOptions.Compiled)]
    private static partial Regex MyUsernameRegex();
}