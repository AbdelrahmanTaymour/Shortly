using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Persistence;

public class UsageRepositoryTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task<OrganizationUsage> OrgUsage(Guid organizationId) =>
        Query(db => db.Set<OrganizationUsage>().SingleAsync(u => u.OrganizationId == organizationId));

    private Task<UserUsage> UserUsageOf(Guid userId) =>
        Query(db => db.Set<UserUsage>().SingleAsync(u => u.UserId == userId));

    [Fact]
    public async Task IncrementLinksCreatedAsync_Should_AddOneToTheMonthlyAndTotalCounters()
    {
        // Arrange
        var org = await Data.OrganizationAsync();

        // Act
        var changed = await WithRepository<IOrganizationUsageRepository, bool>(r => r.IncrementLinksCreatedAsync(org.Id), save: false);

        // Assert
        changed.Should().BeTrue();
        var usage = await OrgUsage(org.Id);
        usage.MonthlyLinksCreated.Should().Be(1);
        usage.TotalLinksCreated.Should().Be(1);
    }

    [Fact]
    public async Task IncrementQrCodesCreatedAsync_Should_AddOneToTheMonthlyAndTotalCounters()
    {
        // Arrange
        var org = await Data.OrganizationAsync();

        // Act
        await WithRepository<IOrganizationUsageRepository, bool>(r => r.IncrementQrCodesCreatedAsync(org.Id), save: false);

        // Assert
        var usage = await OrgUsage(org.Id);
        usage.MonthlyQrCodesCreated.Should().Be(1);
        usage.TotalQrCodesCreated.Should().Be(1);
    }

    [Fact]
    public async Task Increment_Should_ReturnFalse_ForAnUnknownOrganization()
    {
        // Act / Assert
        (await WithRepository<IOrganizationUsageRepository, bool>(r => r.IncrementLinksCreatedAsync(Guid.NewGuid()), save: false)).Should().BeFalse();
        (await WithRepository<IOrganizationUsageRepository, bool>(r => r.IncrementQrCodesCreatedAsync(Guid.NewGuid()), save: false)).Should().BeFalse();
    }

    [Fact]
    public async Task Increment_Should_CountEveryConcurrentLink()
    {
        // Arrange: the quota counter must not lose updates when links are created at the same time
        var org = await Data.OrganizationAsync();

        // Act
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            WithRepository<IOrganizationUsageRepository, bool>(r => r.IncrementLinksCreatedAsync(org.Id), save: false)));

        // Assert
        (await OrgUsage(org.Id)).MonthlyLinksCreated.Should().Be(10);
    }

    [Fact]
    public async Task ResetAllMonthlyAsync_Should_ZeroTheMonthlyCounters_OfOrganizationsWhoseResetDateHasPassed_KeepingTotals()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        await WithRepository<IOrganizationUsageRepository, bool>(r => r.IncrementLinksCreatedAsync(org.Id), save: false);
        var resetAt = TestData.Now.AddMonths(2); // later than the one-month window the usage row starts with

        // Act
        await WithRepository<IOrganizationUsageRepository, int>(r => r.ResetAllMonthlyAsync(resetAt), save: false);

        // Assert
        var usage = await OrgUsage(org.Id);
        usage.MonthlyLinksCreated.Should().Be(0);
        usage.TotalLinksCreated.Should().Be(1);
        usage.MonthlyResetDate.Should().BeCloseTo(resetAt.AddMonths(1), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ResetAllMonthlyAsync_Should_LeaveAnOrganizationAlone_WhoseResetDateIsStillInTheFuture()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        await WithRepository<IOrganizationUsageRepository, bool>(r => r.IncrementLinksCreatedAsync(org.Id), save: false);

        // Act
        await WithRepository<IOrganizationUsageRepository, int>(r => r.ResetAllMonthlyAsync(TestData.Now), save: false);

        // Assert
        (await OrgUsage(org.Id)).MonthlyLinksCreated.Should().Be(1);
    }

    [Fact]
    public async Task UserResetAllMonthlyAsync_Should_ZeroEveryUsersMonthlyCounters_KeepingTotals()
    {
        // Arrange
        var user = await Data.UserAsync();
        await Seed(async db =>
        {
            var loaded = await db.Set<User>().Include(u => u.Usage).SingleAsync(u => u.Id == user.Id);
            loaded.Usage.IncrementLinksCreated(TestData.Now);
            loaded.Usage.IncrementQrCodesCreated(TestData.Now);
        });

        // Act
        var affected = await WithRepository<IUserUsageRepository, int>(r => r.ResetAllMonthlyAsync(), save: false);

        // Assert
        affected.Should().BeGreaterThanOrEqualTo(1);
        var usage = await UserUsageOf(user.Id);
        usage.MonthlyLinksCreated.Should().Be(0);
        usage.MonthlyQrCodesCreated.Should().Be(0);
        usage.TotalLinksCreated.Should().Be(1);
        usage.TotalQrCodesCreated.Should().Be(1);
        usage.MonthlyResetDate.Should().BeAfter(TestData.Now.AddDays(27));
    }
}
