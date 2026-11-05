using Shortly.Domain.UnitTests.TestData;

namespace Shortly.Domain.UnitTests.Users;

public class UserUsageTests
{
    private static readonly DateTime Now = TestClock.Now;
    private static readonly DateTime ResetDate = TestClock.Now.AddMonths(1);

    private static Domain.Users.UserUsage NewUsage() => UserData.CreateUser().Usage;

    [Fact]
    public void NewUsage_Should_StartAtZero_WithTheResetDateOneMonthAhead()
    {
        // Act
        var usage = NewUsage();

        // Assert
        usage.MonthlyLinksCreated.Should().Be(0);
        usage.MonthlyQrCodesCreated.Should().Be(0);
        usage.TotalLinksCreated.Should().Be(0);
        usage.TotalQrCodesCreated.Should().Be(0);
        usage.MonthlyResetDate.Should().Be(ResetDate);
    }

    // ---- incrementing ------------------------------------------------------------------------------------------------

    [Fact]
    public void IncrementLinksCreated_Should_IncreaseMonthlyAndTotalCounters()
    {
        // Arrange
        var usage = NewUsage();

        // Act
        usage.IncrementLinksCreated(Now);
        usage.IncrementLinksCreated(Now);

        // Assert
        usage.MonthlyLinksCreated.Should().Be(2);
        usage.TotalLinksCreated.Should().Be(2);
        usage.TotalQrCodesCreated.Should().Be(0);
    }

    [Fact]
    public void IncrementQrCodesCreated_Should_IncreaseMonthlyAndTotalCounters()
    {
        // Arrange
        var usage = NewUsage();

        // Act
        usage.IncrementQrCodesCreated(Now);

        // Assert
        usage.MonthlyQrCodesCreated.Should().Be(1);
        usage.TotalQrCodesCreated.Should().Be(1);
        usage.MonthlyLinksCreated.Should().Be(0);
    }

    [Fact]
    public void IncrementLinksCreated_Should_NotReset_BeforeTheResetDate()
    {
        // Arrange
        var usage = NewUsage();
        usage.IncrementLinksCreated(Now);

        // Act
        usage.IncrementLinksCreated(ResetDate.AddTicks(-1));

        // Assert
        usage.MonthlyLinksCreated.Should().Be(2);
        usage.MonthlyResetDate.Should().Be(ResetDate);
    }

    [Fact]
    public void IncrementLinksCreated_Should_StartANewMonth_AtTheResetDate_KeepingTotals()
    {
        // Arrange
        var usage = NewUsage();
        usage.IncrementLinksCreated(Now);
        usage.IncrementLinksCreated(Now);
        usage.IncrementQrCodesCreated(Now);

        // Act
        usage.IncrementLinksCreated(ResetDate);

        // Assert
        usage.MonthlyLinksCreated.Should().Be(1);
        usage.MonthlyQrCodesCreated.Should().Be(0);
        usage.TotalLinksCreated.Should().Be(3);
        usage.TotalQrCodesCreated.Should().Be(1);
        usage.MonthlyResetDate.Should().Be(ResetDate.AddMonths(1));
    }

    [Fact]
    public void IncrementQrCodesCreated_Should_StartANewMonth_AtTheResetDate()
    {
        // Arrange
        var usage = NewUsage();
        usage.IncrementLinksCreated(Now);
        usage.IncrementQrCodesCreated(Now);

        // Act
        usage.IncrementQrCodesCreated(ResetDate);

        // Assert
        usage.MonthlyQrCodesCreated.Should().Be(1);
        usage.MonthlyLinksCreated.Should().Be(0);
        usage.MonthlyResetDate.Should().Be(ResetDate.AddMonths(1));
    }

    [Fact]
    public void ResetMonthly_Should_ClearMonthlyCounters_KeepTotals_AndMoveTheResetDate()
    {
        // Arrange
        var usage = NewUsage();
        usage.IncrementLinksCreated(Now);
        usage.IncrementQrCodesCreated(Now);
        var later = Now.AddDays(10);

        // Act
        usage.ResetMonthly(later);

        // Assert
        usage.MonthlyLinksCreated.Should().Be(0);
        usage.MonthlyQrCodesCreated.Should().Be(0);
        usage.TotalLinksCreated.Should().Be(1);
        usage.TotalQrCodesCreated.Should().Be(1);
        usage.MonthlyResetDate.Should().Be(later.AddMonths(1));
    }

    // ---- limits ------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(-1, 50, false)]
    [InlineData(-5, 50, false)]
    [InlineData(10, 0, false)]
    [InlineData(10, 9, false)]
    [InlineData(10, 10, true)]
    [InlineData(10, 11, true)]
    [InlineData(0, 0, true)]
    public void HasReachedMonthlyLinkLimit_Should_CompareTheMonthlyCountWithTheLimit(int limit, int created, bool expected)
    {
        // Arrange
        var usage = NewUsage();
        for (var i = 0; i < created; i++) usage.IncrementLinksCreated(Now);

        // Act
        var reached = usage.HasReachedMonthlyLinkLimit(limit, Now);

        // Assert
        reached.Should().Be(expected);
    }

    [Fact]
    public void HasReachedMonthlyLinkLimit_Should_BeFalse_OnceTheResetDateHasPassed_WithoutChangingState()
    {
        // Arrange
        var usage = NewUsage();
        for (var i = 0; i < 10; i++) usage.IncrementLinksCreated(Now);

        // Act
        var reached = usage.HasReachedMonthlyLinkLimit(10, ResetDate);

        // Assert
        reached.Should().BeFalse();
        usage.MonthlyLinksCreated.Should().Be(10);
    }

    [Theory]
    [InlineData(-1, 5, int.MaxValue)]
    [InlineData(10, 0, 10)]
    [InlineData(10, 4, 6)]
    [InlineData(10, 10, 0)]
    [InlineData(10, 15, 0)]
    [InlineData(0, 0, 0)]
    public void RemainingMonthlyLinks_Should_ReturnTheUnusedAllowance(int limit, int created, int expected)
    {
        // Arrange
        var usage = NewUsage();
        for (var i = 0; i < created; i++) usage.IncrementLinksCreated(Now);

        // Act
        var remaining = usage.RemainingMonthlyLinks(limit, Now);

        // Assert
        remaining.Should().Be(expected);
    }

    [Fact]
    public void RemainingMonthlyLinks_Should_ReturnTheFullAllowance_OnceTheResetDateHasPassed()
    {
        // Arrange
        var usage = NewUsage();
        for (var i = 0; i < 10; i++) usage.IncrementLinksCreated(Now);

        // Act
        var remaining = usage.RemainingMonthlyLinks(10, ResetDate);

        // Assert
        remaining.Should().Be(10);
    }
}
