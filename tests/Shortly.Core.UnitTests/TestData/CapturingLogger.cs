using Microsoft.Extensions.Logging;

namespace Shortly.Core.UnitTests.TestData;

/// <summary>A logger that records what was written, so behavior tests can assert on log levels and exceptions.</summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public sealed record Entry(LogLevel Level, string Message, Exception? Exception);

    public List<Entry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Entries.Add(new Entry(logLevel, formatter(state, exception), exception));
}
