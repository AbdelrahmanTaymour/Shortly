using Microsoft.EntityFrameworkCore;
using Shortly.Core.Admin.GetUserDetail;
using Shortly.Core.Admin.SearchUsers;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Admin;

public class UserQueriesTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    // ---- get user detail --------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetUserDetail_Should_ReturnTheFullAdminView_WhenLookedUpById()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Professional);
        As(Caller.Admin());

        // Act
        var result = await Send(new GetUserDetailQuery(user.Id, null, null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
        result.Value.Username.Should().Be(user.Username);
        result.Value.SubscriptionPlan.Should().Be(enSubscriptionPlan.Professional);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserDetail_Should_ReturnTheSameUser_WhenLookedUpByEmailOrUsername()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(Caller.Admin());

        // Act
        var byEmail = await Send(new GetUserDetailQuery(null, user.Email, null));
        var byUsername = await Send(new GetUserDetailQuery(null, null, user.Username));

        // Assert
        byEmail.Value.Id.Should().Be(user.Id);
        byUsername.Value.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetUserDetail_Should_Fail_WhenNoUserMatches()
    {
        // Arrange
        As(Caller.Admin());

        // Act
        var result = await Send(new GetUserDetailQuery(Guid.NewGuid(), null, null));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    // ---- search users -----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task SearchUsers_Should_FilterByStatus()
    {
        // Arrange
        var active = await Data.UserAsync();
        var inactive = await Data.UserAsync(isActive: false);
        As(Caller.Admin());

        // Act
        var result = await Send(new SearchUsersQuery(active.Email, UserStatusFilter.Active, null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(u => u.Id == active.Id);

        var inactiveSearch = await Send(new SearchUsersQuery(inactive.Email, UserStatusFilter.Active, null));
        inactiveSearch.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchUsers_Should_FilterByRole()
    {
        // Arrange
        var admin = await Data.UserAsync();
        await Seed(async db =>
        {
            var tracked = await db.Set<User>().SingleAsync(u => u.Id == admin.Id);
            tracked.ChangeRole(UserRole.SystemAdmin, TestData.Now);
        });
        As(Caller.Admin());

        // Act
        var result = await Send(new SearchUsersQuery(admin.Email, UserStatusFilter.All, UserRole.SystemAdmin));

        // Assert
        result.Value.Items.Should().ContainSingle(u => u.Id == admin.Id);
    }

    [Fact]
    public async Task SearchUsers_Should_MatchByPartialEmailOrUsername()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(Caller.Admin());

        // Act
        var result = await Send(new SearchUsersQuery(user.Username[..6], UserStatusFilter.All, null));

        // Assert
        result.Value.Items.Should().Contain(u => u.Id == user.Id);
    }

    [Fact]
    public async Task SearchUsers_Should_ClampAnOversizedPageSize()
    {
        // Arrange
        As(Caller.Admin());

        // Act
        var result = await Send(new SearchUsersQuery(null, UserStatusFilter.All, null, Page: 1, PageSize: 500));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().Be(50);
    }
}