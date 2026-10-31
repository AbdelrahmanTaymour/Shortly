using FluentValidation;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.CreateShortUrl;

internal sealed class CreateShortUrlCommandValidator : AbstractValidator<CreateShortUrlCommand>
{
    public CreateShortUrlCommandValidator()
    {
        RuleFor(x => x.OriginalUrl).NotEmpty();
        RuleFor(x => x.CustomShortCode)
            .Length(ShortCode.MinLength, ShortCode.MaxLength)
            .When(x => x.CustomShortCode is not null);
    }
}