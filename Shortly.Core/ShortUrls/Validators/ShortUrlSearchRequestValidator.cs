using FluentValidation;
using Shortly.Core.ShortUrls.DTOs;

namespace Shortly.Core.ShortUrls.Validators;


public class ShortUrlSearchRequestValidator : AbstractValidator<ShortUrlSearchRequest>
{
    public ShortUrlSearchRequestValidator()
    {
        
        // Search Text Validation (Optional: limit length to prevent abuse)
        RuleFor(x => x.Search)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Search))
            .WithMessage("Search term is too long.");
        
        // Status Validation (Case-insensitive whitelist)
        var validStatuses = new[] { "active", "inactive" };
        RuleFor(x => x.Status)
            .Must(x => string.IsNullOrEmpty(x) || validStatuses.Contains(x.ToLower()))
            .WithMessage("Status must be either 'active' or 'inactive'.");

        // Visibility Validation (Case-insensitive whitelist)
        var validVisibilities = new[] { "public", "private" };
        RuleFor(x => x.Visibility)
            .Must(x => string.IsNullOrEmpty(x) || validVisibilities.Contains(x.ToLower()))
            .WithMessage("Visibility must be either 'public' or 'private'.");

        // Date Range Validation
        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom!.Value)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("The 'DateTo' field must be greater than or equal to 'DateFrom'.");

        // Sorting Validation (Case-insensitive whitelist)
        var validSortOptions = new[] { "newest", "oldest", "most-clicks", "least-clicks" };
        RuleFor(x => x.SortBy)
            .Must(x => validSortOptions.Contains(x.ToLower()))
            .WithMessage($"SortBy must be one of the following: {string.Join(", ", validSortOptions)}.");

        // Pagination Validation
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}
