using Shortly.Domain.UnitTests.TestData;
using Shortly.Domain.Users;

namespace Shortly.Domain.UnitTests.Users;

public class UserAuditLogTests
{
    [Fact]
    public void Record_Should_CaptureEveryDetailOfTheEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var log = UserAuditLog.Record(userId, "Login", "from the web app", "203.0.113.5", "Mozilla/5.0", TestClock.Now);

        // Assert
        log.Id.Should().NotBeEmpty();
        log.UserId.Should().Be(userId);
        log.Action.Should().Be("Login");
        log.Details.Should().Be("from the web app");
        log.IpAddress.Should().Be("203.0.113.5");
        log.UserAgent.Should().Be("Mozilla/5.0");
        log.TimeStampUtc.Should().Be(TestClock.Now);
    }

    [Fact]
    public void Record_Should_AllowMissingOptionalDetails()
    {
        // Act
        var log = UserAuditLog.Record(Guid.NewGuid(), "Logout", null, null, null, TestClock.Now);

        // Assert
        log.Details.Should().BeNull();
        log.IpAddress.Should().BeNull();
        log.UserAgent.Should().BeNull();
    }

    [Fact]
    public void Record_Should_GenerateAUniqueIdPerEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var first = UserAuditLog.Record(userId, "Login", null, null, null, TestClock.Now);
        var second = UserAuditLog.Record(userId, "Login", null, null, null, TestClock.Now);

        // Assert
        first.Id.Should().NotBe(second.Id);
    }
}
