using Shortly.Domain.Invitations;
using Shortly.Domain.Members;
using Shortly.Domain.UnitTests.TestData;

namespace Shortly.Domain.UnitTests.Invitations;

public class OrganizationInvitationTests
{
    private static readonly DateTime Now = TestClock.Now;
    private static readonly DateTime ExpiresAt = TestClock.Now.Add(InvitationData.Validity);

    // ---- statuses ----------------------------------------------------------------------------------------------------

    [Fact]
    public void Status_Should_BeTheFourBusinessStatuses()
    {
        // Assert
        Enum.GetNames<enInvitationStatus>().Should().BeEquivalentTo("Pending", "Accepted", "Expired", "Cancelled");
    }

    // ---- creation ----------------------------------------------------------------------------------------------------

    [Fact]
    public void Create_Should_StartAPendingInvitation_WithThePermissionsOfTheRole()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var invitedBy = Guid.NewGuid();

        // Act
        var result = OrganizationInvitation.Create(organizationId, "invitee@example.com", MemberRole.Viewer,
            invitedBy, "token-hash", ExpiresAt, Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var invitation = result.Value;
        invitation.Id.Should().NotBeEmpty();
        invitation.OrganizationId.Should().Be(organizationId);
        invitation.InvitedUserEmail.Should().Be("invitee@example.com");
        invitation.InvitedUserRole.Should().Be(MemberRole.Viewer);
        invitation.InvitedUserPermissions.Should().Be(MemberRolePermissions.For(MemberRole.Viewer));
        invitation.InvitedBy.Should().Be(invitedBy);
        invitation.TokenHash.Should().Be("token-hash");
        invitation.Status.Should().Be(enInvitationStatus.Pending);
        invitation.ExpiresAt.Should().Be(ExpiresAt);
        invitation.CreatedAt.Should().Be(Now);
        invitation.RegisteredAt.Should().BeNull();
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    [InlineData(MemberRole.OrgAdmin)]
    public void Create_Should_DerivePermissionsFromTheRole(MemberRole role)
    {
        // Act
        var invitation = InvitationData.CreateInvitation(role: role);

        // Assert
        invitation.InvitedUserPermissions.Should().Be(MemberRolePermissions.For(role));
    }

    [Fact]
    public void Create_Should_GenerateADifferentIdForEachInvitation()
    {
        // Act
        var first = InvitationData.CreateInvitation();
        var second = InvitationData.CreateInvitation();

        // Assert
        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Create_Should_Fail_When_TheRoleIsOwner()
    {
        // Act
        var result = OrganizationInvitation.Create(Guid.NewGuid(), "invitee@example.com", MemberRole.OrgOwner,
            Guid.NewGuid(), "token-hash", ExpiresAt, Now);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.CannotInviteAsOwner);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_TheEmailIsBlank(string? email)
    {
        // Act
        var result = OrganizationInvitation.Create(Guid.NewGuid(), email!, MemberRole.Member, Guid.NewGuid(),
            "token-hash", ExpiresAt, Now);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.EmailRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_TheTokenHashIsBlank(string? tokenHash)
    {
        // Act
        var result = OrganizationInvitation.Create(Guid.NewGuid(), "invitee@example.com", MemberRole.Member,
            Guid.NewGuid(), tokenHash!, ExpiresAt, Now);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.TokenHashRequired);
    }

    [Fact]
    public void Create_Should_Fail_When_TheOrganizationIsAnEmptyGuid()
    {
        // Act
        var result = OrganizationInvitation.Create(Guid.Empty, "invitee@example.com", MemberRole.Member,
            Guid.NewGuid(), "token-hash", ExpiresAt, Now);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidReference);
    }

    [Fact]
    public void Create_Should_Fail_When_TheInviterIsAnEmptyGuid()
    {
        // Act
        var result = OrganizationInvitation.Create(Guid.NewGuid(), "invitee@example.com", MemberRole.Member,
            Guid.Empty, "token-hash", ExpiresAt, Now);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidReference);
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(0)]
    public void Create_Should_Fail_When_TheExpiryIsNotInTheFuture(int secondsFromNow)
    {
        // Act
        var result = OrganizationInvitation.Create(Guid.NewGuid(), "invitee@example.com", MemberRole.Member,
            Guid.NewGuid(), "token-hash", Now.AddSeconds(secondsFromNow), Now);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidExpiration);
    }

    // ---- expiry ------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void IsExpired_Should_TreatTheExactExpiryInstantAsExpired(int secondsRelativeToExpiry, bool expected)
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();

        // Act
        var expired = invitation.IsExpired(ExpiresAt.AddSeconds(secondsRelativeToExpiry));

        // Assert
        expired.Should().Be(expected);
    }

    [Fact]
    public void IsActionable_Should_BeTrue_ForAPendingInvitation_BeforeItExpires()
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();

        // Assert
        invitation.IsActionable(Now).Should().BeTrue();
    }

    [Fact]
    public void IsActionable_Should_BeFalse_AtAndAfterTheExpiry()
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();

        // Assert
        invitation.IsActionable(ExpiresAt).Should().BeFalse();
        invitation.IsActionable(ExpiresAt.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void IsActionable_Should_BeFalse_ForEveryNonPendingStatus()
    {
        // Assert
        InvitationData.CreateAcceptedInvitation().IsActionable(Now).Should().BeFalse();
        InvitationData.CreateCancelledInvitation().IsActionable(Now).Should().BeFalse();
        InvitationData.CreateExpiredInvitation().IsActionable(Now).Should().BeFalse();
    }

    [Fact]
    public void MarkExpired_Should_ExpireAPendingInvitation_ThatReachedItsExpiry()
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();

        // Act
        var result = invitation.MarkExpired(ExpiresAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(enInvitationStatus.Expired);
    }

    [Fact]
    public void MarkExpired_Should_Fail_BeforeTheExpiry()
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();

        // Act
        var result = invitation.MarkExpired(ExpiresAt.AddTicks(-1));

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidState);
        invitation.Status.Should().Be(enInvitationStatus.Pending);
    }

    [Fact]
    public void MarkExpired_Should_Fail_ForAnInvitationThatIsNotPending()
    {
        // Arrange
        var accepted = InvitationData.CreateAcceptedInvitation();
        var cancelled = InvitationData.CreateCancelledInvitation();

        // Act
        var afterExpiry = ExpiresAt.AddDays(1);

        // Assert
        accepted.MarkExpired(afterExpiry).Error.Should().Be(OrganizationInvitationErrors.InvalidState);
        cancelled.MarkExpired(afterExpiry).Error.Should().Be(OrganizationInvitationErrors.InvalidState);
        accepted.Status.Should().Be(enInvitationStatus.Accepted);
        cancelled.Status.Should().Be(enInvitationStatus.Cancelled);
    }

    // ---- accepting and rejecting -------------------------------------------------------------------------------------

    [Fact]
    public void Accept_Should_MarkAPendingInvitationAccepted_AndStoreTheTime()
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();
        var later = Now.AddHours(2);

        // Act
        var result = invitation.Accept(later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(enInvitationStatus.Accepted);
        invitation.RegisteredAt.Should().Be(later);
    }

    [Fact]
    public void Accept_Should_Fail_AtTheExpiryInstant_AndLeaveTheInvitationUntouched()
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();

        // Act
        var result = invitation.Accept(ExpiresAt);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidOrExpired);
        invitation.Status.Should().Be(enInvitationStatus.Pending);
        invitation.RegisteredAt.Should().BeNull();
    }

    [Fact]
    public void Accept_Should_Fail_WhenCalledTwice()
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();
        invitation.Accept(Now);

        // Act
        var result = invitation.Accept(Now.AddMinutes(1));

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidOrExpired);
        invitation.RegisteredAt.Should().Be(Now);
    }

    [Fact]
    public void Accept_Should_Fail_ForACancelledOrExpiredInvitation()
    {
        // Assert
        InvitationData.CreateCancelledInvitation().Accept(Now).Error
            .Should().Be(OrganizationInvitationErrors.InvalidOrExpired);
        InvitationData.CreateExpiredInvitation().Accept(Now).Error
            .Should().Be(OrganizationInvitationErrors.InvalidOrExpired);
    }

    [Fact]
    public void Reject_Should_CancelAnActionableInvitation()
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();

        // Act
        var result = invitation.Reject(Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(enInvitationStatus.Cancelled);
    }

    [Fact]
    public void Reject_Should_Fail_WhenTheInvitationHasExpired()
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();

        // Act
        var result = invitation.Reject(ExpiresAt);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidOrExpired);
        invitation.Status.Should().Be(enInvitationStatus.Pending);
    }

    [Fact]
    public void Reject_Should_Fail_AfterTheInvitationWasAccepted()
    {
        // Arrange
        var invitation = InvitationData.CreateAcceptedInvitation();

        // Act
        var result = invitation.Reject(Now);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidOrExpired);
        invitation.Status.Should().Be(enInvitationStatus.Accepted);
    }

    // ---- cancelling --------------------------------------------------------------------------------------------------

    [Fact]
    public void Cancel_Should_CancelAPendingInvitation()
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();

        // Act
        var result = invitation.Cancel(Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(enInvitationStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Should_Fail_ForAnAcceptedInvitation()
    {
        // Arrange
        var invitation = InvitationData.CreateAcceptedInvitation();

        // Act
        var result = invitation.Cancel(Now);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.AlreadyAccepted);
        invitation.Status.Should().Be(enInvitationStatus.Accepted);
    }

    [Fact]
    public void Cancel_Should_Fail_ForAnExpiredInvitation_EvenBeforeItWasMarkedExpired()
    {
        // Arrange
        var markedExpired = InvitationData.CreateExpiredInvitation();
        var overdueButPending = InvitationData.CreateInvitation();

        // Act
        var first = markedExpired.Cancel(ExpiresAt.AddDays(1));
        var second = overdueButPending.Cancel(ExpiresAt);

        // Assert
        first.Error.Should().Be(OrganizationInvitationErrors.AlreadyExpired);
        second.Error.Should().Be(OrganizationInvitationErrors.AlreadyExpired);
        markedExpired.Status.Should().Be(enInvitationStatus.Expired);
        overdueButPending.Status.Should().Be(enInvitationStatus.Pending);
    }

    [Fact]
    public void Cancel_Should_Fail_WhenCalledTwice()
    {
        // Arrange
        var invitation = InvitationData.CreateCancelledInvitation();

        // Act
        var result = invitation.Cancel(Now);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.AlreadyCancelled);
    }

    // ---- resending ---------------------------------------------------------------------------------------------------

    [Fact]
    public void Regenerate_Should_ReplaceTheTokenAndExpiry_OfAPendingInvitation()
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();
        var newExpiry = Now.AddDays(14);

        // Act
        var result = invitation.Regenerate("new-token-hash", newExpiry, Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.TokenHash.Should().Be("new-token-hash");
        invitation.ExpiresAt.Should().Be(newExpiry);
        invitation.Status.Should().Be(enInvitationStatus.Pending);
    }

    [Fact]
    public void Regenerate_Should_MakeAnExpiredInvitationPendingAgain()
    {
        // Arrange
        var invitation = InvitationData.CreateExpiredInvitation();
        var afterExpiry = ExpiresAt.AddDays(1);

        // Act
        var result = invitation.Regenerate("new-token-hash", afterExpiry.AddDays(7), afterExpiry);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(enInvitationStatus.Pending);
        invitation.IsActionable(afterExpiry).Should().BeTrue();
    }

    [Fact]
    public void Regenerate_Should_Fail_ForAnAcceptedOrCancelledInvitation()
    {
        // Arrange
        var accepted = InvitationData.CreateAcceptedInvitation();
        var cancelled = InvitationData.CreateCancelledInvitation();

        // Assert
        accepted.Regenerate("new", Now.AddDays(14), Now).Error.Should().Be(OrganizationInvitationErrors.InvalidState);
        cancelled.Regenerate("new", Now.AddDays(14), Now).Error.Should().Be(OrganizationInvitationErrors.InvalidState);
        accepted.TokenHash.Should().Be("token-hash");
        cancelled.TokenHash.Should().Be("token-hash");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Regenerate_Should_Fail_When_TheNewTokenIsBlank(string? tokenHash)
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();

        // Act
        var result = invitation.Regenerate(tokenHash!, Now.AddDays(14), Now);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.TokenHashRequired);
        invitation.TokenHash.Should().Be("token-hash");
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(0)]
    public void Regenerate_Should_Fail_When_TheNewExpiryIsNotInTheFuture(int daysFromNow)
    {
        // Arrange
        var invitation = InvitationData.CreateInvitation();

        // Act
        var result = invitation.Regenerate("new-token-hash", Now.AddDays(daysFromNow), Now);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidExpiration);
        invitation.ExpiresAt.Should().Be(ExpiresAt);
    }
}
