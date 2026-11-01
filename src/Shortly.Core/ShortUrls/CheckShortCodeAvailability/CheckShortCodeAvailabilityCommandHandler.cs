using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.CheckShortCodeAvailability;

internal sealed class CheckShortCodeAvailabilityCommandHandler(
    IShortUrlRepository shortUrlRepository)
    : ICommandHandler<CheckShortCodeAvailabilityCommand, bool>
{
    public async Task<Result<bool>> Handle(CheckShortCodeAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var codeResult = ShortCode.Create(request.ShortCode);

        if (codeResult.IsFailure) return Result.Failure<bool>(codeResult.Error);

        var exists = await shortUrlRepository.ExistsByShortCodeAsync(codeResult.Value, cancellationToken);

        return !exists;
    }
}