using Shortly.Core.Abstractions.Clock;

namespace Shortly.Infrastructure.Clock;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}