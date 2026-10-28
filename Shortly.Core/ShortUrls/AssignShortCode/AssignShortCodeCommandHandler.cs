using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Exceptions;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.AssignShortCode;

internal sealed class AssignShortCodeCommandHandler(
    IShortUrlRepository shortUrlRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AssignShortCodeCommand>
{
    public async Task<Result> Handle(AssignShortCodeCommand request, CancellationToken cancellationToken)
    {
        var shortUrl = await shortUrlRepository.GetByIdAsync(request.ShortUrlId, cancellationToken);
        if (shortUrl is null) return Result.Failure(ShortUrlErrors.NotFound);

        if (!ShortUrlAuthorization.CanManage(shortUrl, userContext))
            return Result.Failure(ShortUrlErrors.NotFound);

        var codeResult = ShortCode.Create(request.NewShortCode);
        if (codeResult.IsFailure) return Result.Failure(codeResult.Error);

        if (await shortUrlRepository.ExistsByShortCodeAsync(codeResult.Value.Value, cancellationToken))
            return Result.Failure(ShortUrlErrors.ShortCodeAlreadyExists);

        var assignResult = shortUrl.AssignShortCode(codeResult.Value, dateTimeProvider.UtcNow);
        if (assignResult.IsFailure) return assignResult;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == "ux_short_urls_short_code")
        {
            return Result.Failure(ShortUrlErrors.ShortCodeAlreadyExists);
        }
    }
}