using Microsoft.Extensions.Configuration;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Invitations.ResendInvitation;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests.Invitations;

public class ResendInvitationCommandHandlerTests
{
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly Organization _organization = OrganizationData.Create(name: "Acme");
    private readonly AuthorizationData _authorization = new();
    private readonly IOrganizationInvitationRepository _invitations = Substitute.For<IOrganizationInvitationRepository>();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IEmailNotifier _notifier = Substitute.For<IEmailNotifier>();
    private readonly ITokenHasher _tokenHasher = Substitute.For<ITokenHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ResendInvitationCommandHandler _sut;

    public ResendInvitationCommandHandlerTests()
    {
        _organizations.GetByIdAsync(_organization.Id, Arg.Any<CancellationToken>()).Returns(_organization);
        _tokenHasher.GenerateToken().Returns("new-plain-token");
        _tokenHasher.Hash("new-plain-token").Returns("new-hashed-token");
        _authorization.CallerIs(_organization.Id, _callerId, MemberRole.OrgAdmin);
        _sut = new ResendInvitationCommandHandler(
            CallerData.Create(_callerId), _invitations, _authorization.Service, _organizations, _notifier,
            new ConfigurationBuilder().Build(), _tokenHasher, _unitOfWork, TestClock.Provider());
    }

    private OrganizationInvitation InvitationInTheRepository(Guid? invitedBy = null, Guid? organizationId = null)
    {
        var invitation = InvitationData.Pending(organizationId ?? _organization.Id, invitedBy);
        _invitations.GetByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);
        return invitation;
    }

    private Task<Result> Handle(OrganizationInvitation invitation, Guid? organizationId = null) =>
        _sut.Handle(new ResendInvitationCommand(organizationId ?? _organization.Id, invitation.Id), default);

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheInvitationDoesNotExist()
    {
        // Act
        var result = await _sut.Handle(new ResendInvitationCommand(_organization.Id, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheInvitationBelongsToAnotherOrganization()
    {
        // Arrange
        var invitation = InvitationInTheRepository(organizationId: Guid.NewGuid());

        // Act
        var result = await Handle(invitation, organizationId: invitation.OrganizationId == _organization.Id ? Guid.NewGuid() : _organization.Id);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_When_TheCallerIsNotAMember()
    {
        // Arrange
        var invitation = InvitationInTheRepository(invitedBy: _callerId);
        _authorization.CallerIsNotAMember(_organization.Id);

        // Act
        var result = await Handle(invitation);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        _notifier.DidNotReceiveWithAnyArgs().EnqueueSendUserInvitation(default!, default!, default!, default!, default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotInviter_When_TheCallerIsAMemberWhoDidNotInviteAndIsNotAdmin()
    {
        // Arrange
        var invitation = InvitationInTheRepository(invitedBy: Guid.NewGuid());
        _authorization.CallerIs(_organization.Id, _callerId, MemberRole.Member);

        // Act
        var result = await Handle(invitation);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.NotInviter);
        invitation.TokenHash.Should().Be(InvitationData.TokenHash);
    }

    [Fact]
    public async Task Handle_Should_AllowTheOriginalInviter_EvenWithoutTheAdminRole()
    {
        // Arrange
        var invitation = InvitationInTheRepository(invitedBy: _callerId);
        _authorization.CallerIs(_organization.Id, _callerId, MemberRole.Member);

        // Act
        var result = await Handle(invitation);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheOrganizationNoLongerExists()
    {
        // Arrange
        var invitation = InvitationInTheRepository(invitedBy: _callerId);
        _organizations.GetByIdAsync(_organization.Id, Arg.Any<CancellationToken>()).Returns((Organization?)null);

        // Act
        var result = await Handle(invitation);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_IssueANewTokenAndExpiry_AndSave()
    {
        // Arrange
        var invitation = InvitationInTheRepository(invitedBy: _callerId);

        // Act
        var result = await Handle(invitation);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.TokenHash.Should().Be("new-hashed-token");
        invitation.ExpiresAt.Should().Be(TestClock.UtcNow.AddDays(7));
        invitation.Status.Should().Be(enInvitationStatus.Pending);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_MakeAnExpiredInvitationPendingAgain()
    {
        // Arrange
        var invitation = InvitationInTheRepository(invitedBy: _callerId);
        invitation.MarkExpired(invitation.ExpiresAt);

        // Act
        var result = await Handle(invitation);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(enInvitationStatus.Pending);
    }

    [Fact]
    public async Task Handle_Should_EmailTheNewPlainToken_OnlyAfterSaving()
    {
        // Arrange
        var invitation = InvitationInTheRepository(invitedBy: _callerId);

        // Act
        await Handle(invitation);

        // Assert
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _notifier.EnqueueSendUserInvitation(InvitationData.Email, "invitee", "invitee", "new-plain-token", "Acme");
        });
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidState_AndSendNothing_ForACancelledInvitation()
    {
        // Arrange
        var invitation = InvitationInTheRepository(invitedBy: _callerId);
        invitation.Cancel(TestClock.UtcNow);

        // Act
        var result = await Handle(invitation);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidState);
        _notifier.DidNotReceiveWithAnyArgs().EnqueueSendUserInvitation(default!, default!, default!, default!, default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidState_ForAnAcceptedInvitation()
    {
        // Arrange
        var invitation = InvitationInTheRepository(invitedBy: _callerId);
        invitation.Accept(TestClock.UtcNow);

        // Act
        var result = await Handle(invitation);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidState);
    }
}
