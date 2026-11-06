using Shortly.Core.Admin.ResetAllOrganizationsUsage;
using Shortly.Core.Admin.ResetAllUsersUsage;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Admin;

public class ResetAllUsageCommandHandlerTests
{
    [Fact]
    public async Task ResetAllUsersUsage_Should_ReturnHowManyUsersWereReset()
    {
        // Arrange
        var usage = Substitute.For<IUserUsageRepository>();
        usage.ResetAllMonthlyAsync(Arg.Any<CancellationToken>()).Returns(42);
        var sut = new ResetAllUsersUsageCommandHandler(usage);

        // Act
        var result = await sut.Handle(new ResetAllUsersUsageCommand(), default);

        // Assert
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task ResetAllOrganizationsUsage_Should_ResetAsOfNow_AndReturnTheCount()
    {
        // Arrange
        var usage = Substitute.For<IOrganizationUsageRepository>();
        usage.ResetAllMonthlyAsync(TestClock.UtcNow, Arg.Any<CancellationToken>()).Returns(7);
        var sut = new ResetAllOrganizationsUsageCommandHandler(usage, TestClock.Provider());

        // Act
        var result = await sut.Handle(new ResetAllOrganizationsUsageCommand(), default);

        // Assert
        result.Value.Should().Be(7);
    }
}
