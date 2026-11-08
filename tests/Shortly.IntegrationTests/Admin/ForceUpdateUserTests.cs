using Microsoft.EntityFrameworkCore;
using Shortly.Core.Admin.ForceUpdateUser;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Admin;

public class ForceUpdateUserTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private static ForceUpdateUserCommand Patch(
        Guid userId, string? email = null, string? username = null, UserRole? role = null,
        enSubscriptionPlan? plan = null, bool? isActive = null, string? profileName = null) =>
        new(userId, email, username, role, plan, isActive, profileName, null, null, null, null, null, null);

    private Task<User> Stored(Guid id) => Query(db => db.Set<User>().SingleAsync(u => u.Id == id));

    // ---- self-targeting guard -------------------------------------------------------------------------------------------

    [Fact]
    public async Task ForceUpdateUser_Should_RefuseToDeactivateTheCallersOwnAccount()
    {
        // Arrange
        var admin = await Data.UserAsync();
        As(admin.Id);

        // Act
        var result = await Send(Patch(admin.Id, isActive: false));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.CannotTargetSelf);
        (await Stored(admin.Id)).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ForceUpdateUser_Should_RefuseToDemoteTheCallersOwnRole()
    {
        // Arrange
        var admin = await Data.UserAsync();
        await Seed(async db =>
        {
            var tracked = await db.Set<User>().SingleAsync(u => u.Id == admin.Id);
            tracked.ChangeRole(UserRole.SystemAdmin, TestData.Now);
        });
        As(admin.Id);

        // Act
        var result = await Send(Patch(admin.Id, role: UserRole.User));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.CannotTargetSelf);
    }

    [Fact]
    public async Task ForceUpdateUser_Should_AllowTheCallerToUpdateTheirOwnProfile_WhenRoleAndActivationAreUntouched()
    {
        // Arrange
        var admin = await Data.UserAsync();
        As(admin.Id);

        // Act
        var result = await Send(Patch(admin.Id, profileName: "New Name"));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ---- email change & re-verification ------------------------------------------------------------------------------

    [Fact]
    public async Task ForceUpdateUser_Should_UnconfirmTheEmail_AndIssueAFreshVerificationToken_WhenTheEmailChanges()
    {
        // Arrange
        var target = await Data.UserAsync();
        As(Caller.Admin());
        var newEmail = $"{Guid.NewGuid():N}@example.com";

        // Act
        var result = await Send(Patch(target.Id, email: newEmail));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await Stored(target.Id);
        stored.Email.Should().Be(newEmail);
        stored.IsEmailConfirmed.Should().BeFalse();
        Emails.Sent.Should().ContainSingle(e => e.Kind == "EmailVerification" && e.To == newEmail);
        (await Query(db => db.Set<UserActionToken>().Where(t => t.UserId == target.Id).Select(t => t.TokenType).SingleAsync()))
            .Should().Be(enUserActionTokenType.EmailVerification);
    }

    [Fact]
    public async Task ForceUpdateUser_Should_RejectAnEmailAlreadyInUse_AndSendNoToken()
    {
        // Arrange
        var target = await Data.UserAsync();
        var other = await Data.UserAsync();
        As(Caller.Admin());

        // Act
        var result = await Send(Patch(target.Id, email: other.Email));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.EmailOrUsernameNotUnique);
        Emails.Sent.Should().BeEmpty();
    }

    // ---- idempotent activation ----------------------------------------------------------------------------------------

    [Fact]
    public async Task ForceUpdateUser_Should_TreatActivatingAnAlreadyActiveUser_AsANoOp()
    {
        // Arrange
        var target = await Data.UserAsync();
        As(Caller.Admin());

        // Act
        var result = await Send(Patch(target.Id, isActive: true));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Stored(target.Id)).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ForceUpdateUser_Should_Deactivate_AndChangeRoleAndPlan_AllAtOnce()
    {
        // Arrange
        var target = await Data.UserAsync();
        As(Caller.Admin());

        // Act
        var result = await Send(Patch(target.Id, role: UserRole.SuperAdmin, plan: enSubscriptionPlan.Professional, isActive: false));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await Stored(target.Id);
        stored.Role.Should().Be(UserRole.SuperAdmin);
        stored.SubscriptionPlanId.Should().Be(enSubscriptionPlan.Professional);
        stored.IsActive.Should().BeFalse();
    }

    // ---- not found -------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ForceUpdateUser_Should_Fail_ForAUserThatDoesNotExist()
    {
        // Arrange
        As(Caller.Admin());

        // Act
        var result = await Send(Patch(Guid.NewGuid(), profileName: "Nobody"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
    }
}
