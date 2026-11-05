namespace Shortly.Domain.UnitTests.TestData;

/// <summary>A fixed point in time, so no domain test depends on the real clock.</summary>
internal static class TestClock
{
    public static readonly DateTime Now = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
}
