using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Invitations.CancelInvitation;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests.Invitations;

/// <summary>The inviter or an admin may cancel, and in both cases they must still be an active member.</summary>
public class CancelInvitationCommandHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly IOrganizationInvitationRepository _invitations = Substitute.For<IOrganizationInvitationRepository>();
    private readonly CancelInvitationCommandHandler _sut;

    public CancelInvitationCommandHandlerTests()
    {
        var caller = CallerData.Create(_callerId);
        _sut = new CancelInvitationCommandHandler(
            caller, _invitations, new OrganizationAuthorizationService(caller, _members),
            Substitute.For<IUnitOfWork>(), TestClock.Provider());
    }

    private OrganizationInvitation InvitationBy(Guid invitedBy)
    {
        var invitation = OrganizationInvitation.Create(
            _orgId, "someone@example.com", MemberRole.Member, invitedBy, "hash",
            TestClock.UtcNow.AddDays(7), TestClock.UtcNow).Value;
        _invitations.GetByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);
        return invitation;
    }

    private void CallerIs(MemberRole role) =>
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _callerId, Arg.Any<CancellationToken>())
            .Returns(MemberData.Create(_orgId, _callerId, role));

    private void CallerIsRemovedAdmin() =>
        _members.GetByOrganizationAndUserAsync(_orgId, _callerId, Arg.Any<CancellationToken>())
            .Returns(MemberData.Create(_orgId, _callerId, MemberRole.OrgAdmin, isActive: false));

    [Fact]
    public async Task Handle_Should_Fail_AndKeepTheInvitationPending_When_TheInviterWasRemoved()
    {
        // Arrange: the original inviter used to bypass the membership check entirely
        var invitation = InvitationBy(_callerId);
        CallerIsRemovedAdmin();

        // Act
        var result = await _sut.Handle(new CancelInvitationCommand(_orgId, invitation.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        invitation.Status.Should().Be(enInvitationStatus.Pending);
    }

    [Fact]
    public async Task Handle_Should_CancelTheInvitation_When_TheCallerIsTheActiveInviterWithoutAdminRole()
    {
        // Arrange
        var invitation = InvitationBy(_callerId);
        CallerIs(MemberRole.Member);

        // Act
        var result = await _sut.Handle(new CancelInvitationCommand(_orgId, invitation.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(enInvitationStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotInviter_When_TheCallerIsAMemberWhoDidNotInviteAndIsNotAdmin()
    {
        // Arrange
        var invitation = InvitationBy(Guid.NewGuid());
        CallerIs(MemberRole.Member);

        // Act
        var result = await _sut.Handle(new CancelInvitationCommand(_orgId, invitation.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.NotInviter);
        invitation.Status.Should().Be(enInvitationStatus.Pending);
    }

    [Fact]
    public async Task Handle_Should_CancelTheInvitation_When_TheCallerIsAnAdminWhoDidNotInvite()
    {
        // Arrange
        var invitation = InvitationBy(Guid.NewGuid());
        CallerIs(MemberRole.OrgAdmin);

        // Act
        var result = await _sut.Handle(new CancelInvitationCommand(_orgId, invitation.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(enInvitationStatus.Cancelled);
    }

    // ---- invitation state --------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheInvitationDoesNotExist()
    {
        // Act
        var result = await _sut.Handle(new CancelInvitationCommand(_orgId, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheInvitationBelongsToAnotherOrganization()
    {
        // Arrange
        var invitation = InvitationBy(_callerId);
        CallerIs(MemberRole.OrgAdmin);

        // Act
        var result = await _sut.Handle(new CancelInvitationCommand(Guid.NewGuid(), invitation.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.NotFound);
        invitation.Status.Should().Be(enInvitationStatus.Pending);
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyAccepted_AndKeepTheInvitation_When_ItWasAccepted()
    {
        // Arrange
        var invitation = InvitationBy(_callerId);
        invitation.Accept(TestClock.UtcNow);
        CallerIs(MemberRole.OrgAdmin);

        // Act
        var result = await _sut.Handle(new CancelInvitationCommand(_orgId, invitation.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.AlreadyAccepted);
        invitation.Status.Should().Be(enInvitationStatus.Accepted);
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyExpired_When_TheInvitationReachedItsExpiry()
    {
        // Arrange
        var invitation = OrganizationInvitation.Create(
            _orgId, "someone@example.com", MemberRole.Member, _callerId, "hash",
            TestClock.UtcNow, TestClock.UtcNow.AddMinutes(-1)).Value;
        _invitations.GetByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);
        CallerIs(MemberRole.OrgAdmin);

        // Act
        var result = await _sut.Handle(new CancelInvitationCommand(_orgId, invitation.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.AlreadyExpired);
    }

    [Fact]
    public async Task Handle_Should_NeverDeleteTheInvitationRecord()
    {
        // Arrange
        var invitation = InvitationBy(_callerId);
        CallerIs(MemberRole.OrgAdmin);

        // Act
        await _sut.Handle(new CancelInvitationCommand(_orgId, invitation.Id), default);

        // Assert: the record stays, only its status changes
        invitation.Status.Should().Be(enInvitationStatus.Cancelled);
        _invitations.ReceivedCalls().Select(c => c.GetMethodInfo().Name).Should().NotContain("Remove");
    }
}
