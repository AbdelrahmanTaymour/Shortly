using Microsoft.EntityFrameworkCore;
using Shortly.Core.Admin.ResetAllOrganizationsUsage;
using Shortly.Core.Admin.ResetAllUsersUsage;
using Shortly.Core.Admin.ResetOrganizationUsage;
using Shortly.Core.Admin.ResetUserUsage;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Admin;

public class UsageAdministrationTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task<OrganizationUsage> OrgUsage(Guid organizationId) =>
        Query(db => db.Set<OrganizationUsage>().SingleAsync(u => u.OrganizationId == organizationId));
 
    private Task<UserUsage> UserUsageOf(Guid userId) =>
        Query(db => db.Set<UserUsage>().SingleAsync(u => u.UserId == userId));
 
    private Task GiveOrganizationUsage(Guid organizationId) => Seed(async db =>
    {
        var organization = await db.Set<Organization>().Include(o => o.Usage).SingleAsync(o => o.Id == organizationId);
        organization.Usage.IncrementLinksCreated(TestData.Now);
        organization.Usage.IncrementQrCodesCreated(TestData.Now);
    });
 
    private Task GiveUserUsage(Guid userId) => Seed(async db =>
    {
        var user = await db.Set<User>().Include(u => u.Usage).SingleAsync(u => u.Id == userId);
        user.Usage.IncrementLinksCreated(TestData.Now);
        user.Usage.IncrementQrCodesCreated(TestData.Now);
    });
 
    private Task SetOrganizationResetDate(Guid organizationId, DateTime resetDate) => Seed(db =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE organization_usage SET monthly_reset_date = {resetDate} WHERE organization_id = {organizationId}"));
 
    // ---- ResetUserUsage -------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task ResetUserUsage_Should_ZeroTheUsersMonthlyCounters_AndKeepTheTotals()
    {
        // Arrange
        var user = await Data.UserAsync();
        await GiveUserUsage(user.Id);
        As(Caller.Admin());
 
        // Act
        var result = await Send(new ResetUserUsageCommand(user.Id));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        var usage = await UserUsageOf(user.Id);
        usage.MonthlyLinksCreated.Should().Be(0);
        usage.MonthlyQrCodesCreated.Should().Be(0);
        usage.TotalLinksCreated.Should().Be(1);
        usage.TotalQrCodesCreated.Should().Be(1);
        usage.MonthlyResetDate.Should().BeAfter(TestData.Now.AddDays(27));
    }
 
    [Fact]
    public async Task ResetUserUsage_Should_ReturnNotFound_ForAnUnknownUser()
    {
        // Arrange
        As(Caller.Admin());
 
        // Act
        var result = await Send(new ResetUserUsageCommand(Guid.NewGuid()));
 
        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
    }
 
    // ---- ResetAllUsersUsage ----------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task ResetAllUsersUsage_Should_ZeroEveryUsersMonthlyCounters_RegardlessOfTheirResetDate_AndReturnHowManyChanged()
    {
        // Arrange: this reset is unconditional, unlike the organization one below
        var dueSoon = await Data.UserAsync();
        var notDueYet = await Data.UserAsync();
        await GiveUserUsage(dueSoon.Id);
        await GiveUserUsage(notDueYet.Id);
        As(Caller.Admin());
 
        // Act
        var result = await Send(new ResetAllUsersUsageCommand());
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThanOrEqualTo(2);
        (await UserUsageOf(dueSoon.Id)).MonthlyLinksCreated.Should().Be(0);
        (await UserUsageOf(notDueYet.Id)).MonthlyLinksCreated.Should().Be(0);
    }
 
    // ---- ResetOrganizationUsage ------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task ResetOrganizationUsage_Should_ZeroTheOrganizationsMonthlyCounters_AndKeepTheTotals()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        await GiveOrganizationUsage(org.Id);
        As(Caller.Admin());
 
        // Act
        var result = await Send(new ResetOrganizationUsageCommand(org.Id));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        var usage = await OrgUsage(org.Id);
        usage.MonthlyLinksCreated.Should().Be(0);
        usage.MonthlyQrCodesCreated.Should().Be(0);
        usage.TotalLinksCreated.Should().Be(1);
        usage.TotalQrCodesCreated.Should().Be(1);
        usage.MonthlyResetDate.Should().BeAfter(TestData.Now.AddDays(27));
    }
 
    [Fact]
    public async Task ResetOrganizationUsage_Should_ReturnNotFound_ForAnUnknownOrganization()
    {
        // Arrange
        As(Caller.Admin());
 
        // Act
        var result = await Send(new ResetOrganizationUsageCommand(Guid.NewGuid()));
 
        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
    }
 
    // ---- ResetAllOrganizationsUsage --------------------------------------------------------------------------------------
 
    [Fact]
    public async Task ResetAllOrganizationsUsage_Should_ResetOnlyOrganizationsWhoseResetDateHasPassed_KeepingOthersUntouched()
    {
        // Arrange
        var due = await Data.OrganizationAsync();
        var notDue = await Data.OrganizationAsync();
        await GiveOrganizationUsage(due.Id);
        await GiveOrganizationUsage(notDue.Id);
        await SetOrganizationResetDate(due.Id, TestData.Now.AddDays(-1)); // already past, so this one is due
        As(Caller.Admin());
 
        // Act
        var result = await Send(new ResetAllOrganizationsUsageCommand());
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThanOrEqualTo(1);
        (await OrgUsage(due.Id)).MonthlyLinksCreated.Should().Be(0);
        (await OrgUsage(notDue.Id)).MonthlyLinksCreated.Should().Be(1); // its reset date is still in the future
    }
}
