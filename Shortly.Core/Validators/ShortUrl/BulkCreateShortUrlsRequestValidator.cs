using FluentValidation;
using Shortly.Core.DTOs.ShortUrlDTOs;

namespace Shortly.Core.Validators.ShortUrl;

public class BulkCreateShortUrlsRequestValidator : AbstractValidator<BulkCreateShortUrlsRequest>
{
    public BulkCreateShortUrlsRequestValidator()
    {
        RuleFor(x => x.Requests)
            .NotNull()
            .WithMessage("Requests collection cannot be null.")
            .NotEmpty()
            .WithMessage("Requests collection cannot be empty.")
            .Must(x => x.Count <= 1000)
            .WithMessage("Cannot process more than 1000 URLs in a single bulk operation.");

        RuleForEach(x => x.Requests)
            .SetValidator(new CreateShortUrlRequestValidator());
        
        RuleFor(x => x.Requests)
            .Must(NotContainDuplicateCustomCodes)
            .WithMessage("Duplicate custom short codes found within the request.")
            .When(x => x.Requests.Any(r => !string.IsNullOrWhiteSpace(r.CustomShortCode)));
    }
    
    private static bool NotContainDuplicateCustomCodes(IReadOnlyCollection<CreateShortUrlRequest> requests)
    {
        var customCodes = requests
            .Where(r => !string.IsNullOrWhiteSpace(r.CustomShortCode))
            .Select(r => r.CustomShortCode!)
            .ToList();

        return customCodes.Count == customCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count();
    }
}