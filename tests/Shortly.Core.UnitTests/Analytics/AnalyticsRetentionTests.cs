using Shortly.Core.Analytics;

using Shortly.Core.UnitTests.TestData;

namespace Shortly.Core.UnitTests.Analytics;

public class AnalyticsRetentionTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Unlimited_DoesNotClamp()
    {
        AnalyticsRetention.ClampFrom(null, -1, Now).Should().BeNull();
        AnalyticsRetention.ClampFrom(Now.AddYears(-5), -1, Now).Should().Be(Now.AddYears(-5));
    }

    [Fact]
    public void PositiveDays_LimitTheVisibleWindow_WhenNoStartIsRequested() =>
        AnalyticsRetention.ClampFrom(null, 30, Now).Should().Be(Now.AddDays(-30));

    [Fact]
    public void PositiveDays_ClampAnEarlierRequestedStart() =>
        AnalyticsRetention.ClampFrom(Now.AddDays(-400), 30, Now).Should().Be(Now.AddDays(-30));

    [Fact]
    public void PositiveDays_KeepALaterRequestedStart() =>
        AnalyticsRetention.ClampFrom(Now.AddDays(-5), 30, Now).Should().Be(Now.AddDays(-5));

    [Fact]
    public void Boundary_ARequestedStartExactlyAtTheEdgeIsTheEdge() =>
        AnalyticsRetention.ClampFrom(Now.AddDays(-30), 30, Now).Should().Be(Now.AddDays(-30));

    [Theory]
    [InlineData(null)]
    [InlineData(-400)]
    public void Zero_MeansNoHistory_WhateverWasRequested(int? requestedDaysAgo)
    {
        var requested = requestedDaysAgo is { } days ? Now.AddDays(days) : (DateTime?)null;

        AnalyticsRetention.ClampFrom(requested, 0, Now).Should().Be(Now);
    }
}
