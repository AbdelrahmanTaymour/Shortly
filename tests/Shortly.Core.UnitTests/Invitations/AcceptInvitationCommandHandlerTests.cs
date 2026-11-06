using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Invitations.AcceptInvitation;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Invitations;

public class AcceptInvitationCommandHandlerTests
{
    private static readonly AcceptInvitationCommand Command = new("plain-token");

    private readonly Organization _organization = OrganizationData.Create(memberLimit: 3);
    private readonly User _caller = UserData.Create(email: InvitationData.Email, username: "invitee");
    private readonly Guid _inviterId = Guid.NewGuid();
    private readonly IOrganizationInvitationRepository _invitations = Substitute.For<IOrganizationInvitationRepository>();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AcceptInvitationCommandHandler _sut;

    public AcceptInvitationCommandHandlerTests()
    {
        var tokenHasher = Substitute.For<ITokenHasher>();
        tokenHasher.Hash(Command.Token).Returns(InvitationData.TokenHash);
        _users.GetByIdAsync(_caller.Id, Arg.Any<CancellationToken>()).Returns(_caller);
        _organizations.GetByIdAsync(_organization.Id, Arg.Any<CancellationToken>()).Returns(_organization);
        _sut = new AcceptInvitationCommandHandler(
            CallerData.Create(_caller.Id), _invitations, _members, _organizations, _users, tokenHasher, _unitOfWork,
            TestClock.Provider());
    }

    private OrganizationInvitation InvitationInTheRepository(
        MemberRole role = MemberRole.Member, string email = InvitationData.Email, DateTime? expiresAt = null)
    {
        var invitation = InvitationData.Pending(_organization.Id, _inviterId, email, role, expiresAt);
        _invitations.GetByTokenHashAsync(InvitationData.TokenHash, Arg.Any<CancellationToken>()).Returns(invitation);
        return invitation;
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_NoInvitationMatchesTheToken()
    {
        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmailMismatch_AndConsumeNothing_When_TheCallerIsSignedInWithAnotherEmail()
    {
        // Arrange
        var invitation = InvitationInTheRepository(email: "someone-else@example.com");

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.EmailMismatch);
        invitation.Status.Should().Be(enInvitationStatus.Pending);
        _members.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_CompareTheEmailsIgnoringCase()
    {
        // Arrange
        InvitationInTheRepository(email: "INVITEE@Example.com");

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnEmailMismatch_When_TheCallerNoLongerExists()
    {
        // Arrange
        InvitationInTheRepository();
        _users.GetByIdAsync(_caller.Id, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.EmailMismatch);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheOrganizationNoLongerExists()
    {
        // Arrange
        InvitationInTheRepository();
        _organizations.GetByIdAsync(_organization.Id, Arg.Any<CancellationToken>()).Returns((Organization?)null);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnInactive_AndKeepTheInvitationPending_When_TheOrganizationIsInactive()
    {
        // Arrange
        var invitation = InvitationInTheRepository();
        _organization.Deactivate(TestClock.UtcNow);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.Inactive);
        invitation.Status.Should().Be(enInvitationStatus.Pending);
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyMember_When_TheCallerIsAlreadyAnActiveMember()
    {
        // Arrange
        InvitationInTheRepository();
        _members.GetByOrganizationAndUserAsync(_organization.Id, _caller.Id, Arg.Any<CancellationToken>())
            .Returns(MemberData.Create(_organization.Id, _caller.Id, MemberRole.Member));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.AlreadyMember);
    }

    [Fact]
    public async Task Handle_Should_ReturnMemberLimitReached_AndKeepTheInvitationPendingForALaterRetry_When_TheOrganizationIsFull()
    {
        // Arrange
        var invitation = InvitationInTheRepository();
        _members.GetMemberCountByOrganizationAsync(_organization.Id, Arg.Any<CancellationToken>()).Returns(3);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.MemberLimitReached);
        invitation.Status.Should().Be(enInvitationStatus.Pending);
        _members.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidOrExpired_AndAddNobody_When_TheInvitationHasExpired()
    {
        // Arrange
        InvitationInTheRepository(expiresAt: TestClock.UtcNow);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidOrExpired);
        _members.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidOrExpired_When_TheInvitationWasAlreadyUsed()
    {
        // Arrange
        var invitation = InvitationInTheRepository();
        invitation.Accept(TestClock.UtcNow);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidOrExpired);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    [InlineData(MemberRole.OrgAdmin)]
    public async Task Handle_Should_AddTheCallerWithTheInvitedRole_AcceptTheInvitation_AndSave(MemberRole invitedRole)
    {
        // Arrange
        var invitation = InvitationInTheRepository(invitedRole);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _members.Received(1).Add(Arg.Is<OrganizationMember>(m =>
            m.UserId == _caller.Id && m.OrganizationId == _organization.Id && m.Role == invitedRole
            && m.IsActive && m.InvitedBy == _inviterId));
        invitation.Status.Should().Be(enInvitationStatus.Accepted);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReactivateAFormerMember_WithTheInvitedRole_InsteadOfRestoringTheOldOne()
    {
        // Arrange
        InvitationInTheRepository(MemberRole.Viewer);
        var former = MemberData.Create(_organization.Id, _caller.Id, MemberRole.OrgAdmin, isActive: false);
        _members.GetByOrganizationAndUserAsync(_organization.Id, _caller.Id, Arg.Any<CancellationToken>()).Returns(former);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        former.IsActive.Should().BeTrue();
        former.Role.Should().Be(MemberRole.Viewer);
        former.JoinedAt.Should().Be(TestClock.UtcNow);
        _members.DidNotReceiveWithAnyArgs().Add(default!);
    }
}
