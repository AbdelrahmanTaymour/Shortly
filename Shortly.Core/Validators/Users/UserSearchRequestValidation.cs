using FluentValidation;
using Shortly.Core.DTOs.UsersDTOs.Search;

namespace Shortly.Core.Validators.Users;

public class UserSearchRequestValidation : AbstractValidator<UserSearchRequest>
{
    public UserSearchRequestValidation()
    {
        // Search term validation (optional but if provided, should not exceeds 100 characters)
        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .WithMessage("Search term cannot exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));

        // Subscription plan validation (optional, but if provided must be a valid enum)
        RuleFor(x => x.SubscriptionPlan)
            .IsInEnum()
            .WithMessage("Invalid subscription plan")
            .When(x => x.SubscriptionPlan.HasValue);

        // Page validation - must be positive
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        // Page size validation - reasonable limits
        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100 records");
    }
}