namespace Shortly.Core.Abstractions.Clock;

public interface IDateTimeProvider
{
    public DateTime UtcNow { get; }
}