using Shortly.Core.Abstractions.Clock;

namespace Shortly.Core.UnitTests.TestData;

/// <summary>A fixed point in time, so no handler test depends on the real clock.</summary>
internal static class TestClock
{
    public static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>An <see cref="IDateTimeProvider"/> that always returns <see cref="UtcNow"/>.</summary>
    public static IDateTimeProvider Provider()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(UtcNow);
        return clock;
    }
}
