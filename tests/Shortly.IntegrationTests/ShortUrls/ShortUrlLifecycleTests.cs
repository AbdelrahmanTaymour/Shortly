using Microsoft.EntityFrameworkCore;
using Shortly.Core.ShortUrls.ActivateShortUrl;
using Shortly.Core.ShortUrls.AssignShortCode;
using Shortly.Core.ShortUrls.CheckShortCodeAvailability;
using Shortly.Core.ShortUrls.DeactivateShortUrl;
using Shortly.Core.ShortUrls.DeleteShortUrl;
using Shortly.Core.ShortUrls.RedirectToShortUrl;
using Shortly.Core.ShortUrls.RemoveShortUrlPassword;
using Shortly.Core.ShortUrls.SetShortUrlPassword;
using Shortly.Core.ShortUrls.UpdateShortUrl;
using Shortly.Domain.Members;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.ShortUrls;

public class ShortUrlLifecycleTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task<ShortUrl> Stored(long id) => Query(db => db.Set<ShortUrl>().SingleAsync(s => s.Id == id));
 
    private Task<bool> Exists(long id) => Query(db => db.Set<ShortUrl>().AnyAsync(s => s.Id == id));
 
    private static UpdateShortUrlCommand Update(
        ShortUrl link, string? url = null, string? code = null, int? clickLimit = null, bool? isActive = null) =>
        new(link.Id, url ?? link.OriginalUrl.Value, code ?? link.ShortCode.Value, "New title", "New description",
            clickLimit ?? link.ClickLimit, isActive ?? link.IsActive, link.IsPrivate, link.TrackingEnabled, null);
 
    private static RedirectToShortUrlCommand Redirect(string code, string? password = null) =>
        new(code, password, "203.0.113.9", Guid.NewGuid().ToString("N"), "test-agent", null, null, null, null, null, null);
 
    // The DB is shared across the whole test run, so a fixed literal code risks colliding with another test.
    private static string UniqueCode() => "c" + Guid.NewGuid().ToString("N")[..8];
 
    // ---- Activate / Deactivate --------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task Deactivate_Should_SetTheLinkInactive_ForTheOwner()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
 
        // Act
        var result = await Send(new DeactivateShortUrlCommand(link.Id));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Stored(link.Id)).IsActive.Should().BeFalse();
    }
 
    [Fact]
    public async Task Deactivate_Should_ReturnAlreadyInactive_ForASecondCall()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
        await Send(new DeactivateShortUrlCommand(link.Id));
 
        // Act
        var result = await Send(new DeactivateShortUrlCommand(link.Id));
 
        // Assert
        result.Error.Should().Be(ShortUrlErrors.AlreadyInactive);
    }
 
    [Fact]
    public async Task Activate_Should_SetTheLinkActive_AfterItWasDeactivated()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
        await Send(new DeactivateShortUrlCommand(link.Id));
 
        // Act
        var result = await Send(new ActivateShortUrlCommand(link.Id));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Stored(link.Id)).IsActive.Should().BeTrue();
    }
 
    [Fact]
    public async Task Activate_Should_ReturnAlreadyActive_ForALinkThatIsAlreadyActive()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
 
        // Act
        var result = await Send(new ActivateShortUrlCommand(link.Id));
 
        // Assert
        result.Error.Should().Be(ShortUrlErrors.AlreadyActive);
    }
 
    [Fact]
    public async Task Deactivate_Should_ReturnNotFound_ForAnotherUsersLink()
    {
        // Arrange
        var owner = await Data.UserAsync();
        var link = await Data.UserLinkAsync(owner.Id);
        var stranger = await Data.UserAsync();
        As(stranger.Id);
 
        // Act
        var result = await Send(new DeactivateShortUrlCommand(link.Id));
 
        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound); // 404, not 403 — existence is not revealed
        (await Stored(link.Id)).IsActive.Should().BeTrue();
    }
 
    [Fact]
    public async Task Deactivate_Should_AllowAnOrganizationMember_ButRefuseAViewer()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (memberUser, member) = await Data.MemberAsync(org.Id, MemberRole.Member);
        var (viewerUser, _) = await Data.MemberAsync(org.Id, MemberRole.Viewer);
        var link = await Data.OrganizationLinkAsync(org.Id, member.Id);
 
        // Act
        As(viewerUser.Id, org.Id);
        var asViewer = await Send(new DeactivateShortUrlCommand(link.Id));
        As(memberUser.Id, org.Id);
        var asMember = await Send(new DeactivateShortUrlCommand(link.Id));
 
        // Assert
        asViewer.Error.Should().Be(ShortUrlErrors.NotFound);
        asMember.IsSuccess.Should().BeTrue();
    }
 
    [Fact]
    public async Task Deactivate_Should_AllowASystemAdmin_RegardlessOfOwnership()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(Caller.Admin());
 
        // Act
        var result = await Send(new DeactivateShortUrlCommand(link.Id));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
    }
 
    // ---- Delete ---------------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task Delete_Should_RemoveTheLink_ForTheOwner()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
 
        // Act
        var result = await Send(new DeleteShortUrlCommand(link.Id));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Exists(link.Id)).Should().BeFalse();
    }
 
    [Fact]
    public async Task Delete_Should_ReturnNotFound_ForAnotherUsersLink_AndKeepIt()
    {
        // Arrange
        var owner = await Data.UserAsync();
        var link = await Data.UserLinkAsync(owner.Id);
        var stranger = await Data.UserAsync();
        As(stranger.Id);
 
        // Act
        var result = await Send(new DeleteShortUrlCommand(link.Id));
 
        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
        (await Exists(link.Id)).Should().BeTrue();
    }
 
    [Fact]
    public async Task Delete_Should_ReturnNotFound_ForAnUnknownId()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);
 
        // Act
        var result = await Send(new DeleteShortUrlCommand(long.MaxValue));
 
        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
    }
 
    // ---- Update ---------------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task Update_Should_ChangeTheEditableFields_WithoutTouchingTheShortCode()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
 
        // Act
        var result = await Send(Update(link, url: "https://example.com/updated", clickLimit: 50, isActive: false));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await Stored(link.Id);
        stored.OriginalUrl.Value.Should().Be("https://example.com/updated");
        stored.Title.Should().Be("New title");
        stored.ClickLimit.Should().Be(50);
        stored.IsActive.Should().BeFalse();
        stored.ShortCode.Should().Be(link.ShortCode);
    }
 
    [Fact]
    public async Task Update_Should_RequireCustomShortCodeEntitlement_WhenChangingTheCode()
    {
        // Arrange: Free plan does not include custom short codes
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
 
        // Act
        var result = await Send(Update(link, code: UniqueCode()));
 
        // Assert
        result.Error.Should().Be(ShortUrlErrors.CustomShortCodeNotAllowed);
        (await Stored(link.Id)).ShortCode.Should().Be(link.ShortCode);
    }
 
    [Fact]
    public async Task Update_Should_ChangeTheShortCode_ForAPlanThatAllowsIt()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Professional);
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
        var newCode = UniqueCode();
 
        // Act
        var result = await Send(Update(link, code: newCode));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Stored(link.Id)).ShortCode.Value.Should().Be(newCode);
    }
 
    [Fact]
    public async Task Update_Should_ReturnShortCodeAlreadyExists_ForACodeTakenByAnotherLink()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Professional);
        var link = await Data.UserLinkAsync(user.Id);
        var other = await Data.UserLinkAsync(user.Id, code: UniqueCode());
        As(user.Id);
 
        // Act
        var result = await Send(Update(link, code: other.ShortCode.Value));
 
        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeAlreadyExists);
    }
 
    [Fact]
    public async Task Update_Should_ReturnNotFound_ForAnotherUsersLink()
    {
        // Arrange
        var owner = await Data.UserAsync();
        var link = await Data.UserLinkAsync(owner.Id);
        var stranger = await Data.UserAsync();
        As(stranger.Id);
 
        // Act
        var result = await Send(Update(link, url: "https://example.com/hijacked"));
 
        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
        (await Stored(link.Id)).OriginalUrl.Should().Be(link.OriginalUrl);
    }
 
    // ---- SetPassword / RemovePassword ------------------------------------------------------------------------------------
 
    [Fact]
    public async Task SetPassword_Should_ProtectTheLink_ForAPlanThatAllowsIt()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Professional);
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
 
        // Act
        var result = await Send(new SetShortUrlPasswordCommand(link.Id, "s3cret-pw"));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await Stored(link.Id);
        stored.IsPasswordProtected.Should().BeTrue();
        stored.PasswordHash.Should().NotBeNullOrWhiteSpace();
    }
 
    [Fact]
    public async Task SetPassword_Should_ReturnLinkProtectionNotAllowed_ForTheFreePlan()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
 
        // Act
        var result = await Send(new SetShortUrlPasswordCommand(link.Id, "s3cret-pw"));
 
        // Assert
        result.Error.Should().Be(ShortUrlErrors.LinkProtectionNotAllowed);
        (await Stored(link.Id)).IsPasswordProtected.Should().BeFalse();
    }
 
    [Fact]
    public async Task RemovePassword_Should_ClearProtection()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Professional);
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
        await Send(new SetShortUrlPasswordCommand(link.Id, "s3cret-pw"));
 
        // Act
        var result = await Send(new RemoveShortUrlPasswordCommand(link.Id));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await Stored(link.Id);
        stored.IsPasswordProtected.Should().BeFalse();
        stored.PasswordHash.Should().BeNull();
    }
 
    [Fact]
    public async Task RemovePassword_Should_ReturnNotPasswordProtected_WhenNoneIsSet()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
 
        // Act
        var result = await Send(new RemoveShortUrlPasswordCommand(link.Id));
 
        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotPasswordProtected);
    }
 
    [Fact]
    public async Task SetPasswordThenRemovePassword_Should_ChangeWhatTheRedirectRequires()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Professional);
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
 
        // Act / Assert: protecting the link makes the redirect demand the password
        await Send(new SetShortUrlPasswordCommand(link.Id, "s3cret-pw"));
        (await Send(Redirect(link.ShortCode.Value))).Error.Should().Be(ShortUrlErrors.PasswordRequired);
        (await Send(Redirect(link.ShortCode.Value, "wrong"))).Error.Should().Be(ShortUrlErrors.InvalidPassword);
        (await Send(Redirect(link.ShortCode.Value, "s3cret-pw"))).Value.Should().Be(link.OriginalUrl.Value);
 
        // Act / Assert: removing the password lets an unauthenticated redirect through again
        await Send(new RemoveShortUrlPasswordCommand(link.Id));
        (await Send(Redirect(link.ShortCode.Value))).Value.Should().Be(link.OriginalUrl.Value);
    }
 
    // ---- AssignShortCode --------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task AssignShortCode_Should_ChangeTheCode_ForAPlanThatAllowsIt()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Starter);
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
        var newCode = UniqueCode();
 
        // Act
        var result = await Send(new AssignShortCodeCommand(link.Id, newCode));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Stored(link.Id)).ShortCode.Value.Should().Be(newCode);
    }
 
    [Fact]
    public async Task AssignShortCode_Should_ReturnCustomShortCodeNotAllowed_ForTheFreePlan()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
 
        // Act
        var result = await Send(new AssignShortCodeCommand(link.Id, UniqueCode()));
 
        // Assert
        result.Error.Should().Be(ShortUrlErrors.CustomShortCodeNotAllowed);
        (await Stored(link.Id)).ShortCode.Should().Be(link.ShortCode);
    }
 
    [Fact]
    public async Task AssignShortCode_Should_ReturnShortCodeAlreadyExists_ForATakenCode()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Starter);
        var link = await Data.UserLinkAsync(user.Id);
        var takenCode = UniqueCode();
        var other = await Data.UserLinkAsync(user.Id, code: takenCode);
        As(user.Id);
 
        // Act
        var result = await Send(new AssignShortCodeCommand(link.Id, takenCode));
 
        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeAlreadyExists);
    }
 
    [Fact]
    public async Task AssignShortCode_Should_ReturnNotFound_ForAnotherUsersLink()
    {
        // Arrange
        var owner = await Data.UserAsync(plan: enSubscriptionPlan.Starter);
        var link = await Data.UserLinkAsync(owner.Id);
        var stranger = await Data.UserAsync(plan: enSubscriptionPlan.Starter);
        As(stranger.Id);
 
        // Act
        var result = await Send(new AssignShortCodeCommand(link.Id, UniqueCode()));
 
        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
    }
 
    // ---- CheckShortCodeAvailability -----------------------------------------------------------------------------------------
 
    [Fact]
    public async Task CheckAvailability_Should_ReturnTrue_ForAnUnusedCode()
    {
        // Act
        var result = await Send(new CheckShortCodeAvailabilityCommand(UniqueCode()));
 
        // Assert
        result.Value.Should().BeTrue();
    }
 
    [Fact]
    public async Task CheckAvailability_Should_ReturnFalse_ForACodeAlreadyInUse()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
 
        // Act
        var result = await Send(new CheckShortCodeAvailabilityCommand(link.ShortCode.Value));
 
        // Assert
        result.Value.Should().BeFalse();
    }
}
