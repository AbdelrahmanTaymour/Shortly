using Microsoft.Extensions.Configuration;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Invitations.CancelInvitation;
using Shortly.Core.Invitations.CreateInvitation;
using Shortly.Core.Members.AddMember;
using Shortly.Core.Members.RemoveMember;
using Shortly.Core.Members.UpdateMemberRole;
using Shortly.Core.Teams.CreateTeam;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Teams;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests;

/// <summary>
/// Regression tests: a member who was removed (deactivated) from an organization must lose the permissions of their old
/// role. The handlers authorize through the shared <see cref="OrganizationAuthorizationService"/> (a real instance over a
/// mocked repository), which only trusts an ACTIVE membership.
/// </summary>
public class RemovedMemberAuthorityTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public RemovedMemberAuthorityTests() => _clock.UtcNow.Returns(TestData.UtcNow);

    private IUserContext Caller => TestData.Caller(_callerId);

    private IOrganizationAuthorizationService Authorization => new OrganizationAuthorizationService(Caller, _members);

    /// <summary>The caller was an OrgAdmin but has since been removed: only the unfiltered lookup still sees the row.</summary>
    private void CallerIsRemovedAdmin() =>
        _members.GetByOrganizationAndUserAsync(_orgId, _callerId, Arg.Any<CancellationToken>())
            .Returns(TestData.Member(_orgId, _callerId, MemberRole.OrgAdmin, isActive: false));

    private void CallerIs(MemberRole role) =>
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _callerId, Arg.Any<CancellationToken>())
            .Returns(TestData.Member(_orgId, _callerId, role));

    // ---- AddMember ---------------------------------------------------------------------------

    private AddMemberCommandHandler AddMemberHandler() =>
        new(Caller, OrganizationExists(), Authorization, _members, _unitOfWork, _clock);

    private IOrganizationRepository OrganizationExists()
    {
        var organizations = Substitute.For<IOrganizationRepository>();
        organizations.GetByIdAsync(_orgId, Arg.Any<CancellationToken>()).Returns(
            Organization.Create("Acme", null, null, null, 10, Guid.NewGuid(), enSubscriptionPlan.Free, TestData.UtcNow).Value);
        return organizations;
    }

    [Fact]
    public async Task AddMember_ByRemovedAdmin_IsDenied()
    {
        CallerIsRemovedAdmin();

        var result = await AddMemberHandler().Handle(new AddMemberCommand(_orgId, Guid.NewGuid(), MemberRole.Member), default);

        result.Error.Should().Be(OrganizationErrors.NotAMember);
        _members.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task AddMember_ByActiveMemberBelowAdmin_IsDeniedWithInsufficientRole(MemberRole role)
    {
        CallerIs(role);

        var result = await AddMemberHandler().Handle(new AddMemberCommand(_orgId, Guid.NewGuid(), MemberRole.Member), default);

        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
    }

    [Fact]
    public async Task AddMember_ByActiveAdmin_PassesAuthorization()
    {
        CallerIs(MemberRole.OrgAdmin);

        var result = await AddMemberHandler().Handle(new AddMemberCommand(_orgId, Guid.NewGuid(), MemberRole.Member), default);

        result.IsSuccess.Should().BeTrue();
        _members.Received(1).Add(Arg.Any<OrganizationMember>());
    }

    // ---- RemoveMember / UpdateMemberRole -----------------------------------------------------

    [Fact]
    public async Task RemoveMember_ByRemovedAdmin_IsDenied()
    {
        CallerIsRemovedAdmin();

        var result = await new RemoveMemberCommandHandler(Authorization, _members, _unitOfWork)
            .Handle(new RemoveMemberCommand(_orgId, Guid.NewGuid()), default);

        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task RemoveMember_ByActiveAdmin_StillFindsInactiveTargetToReportAlreadyInactive()
    {
        // Target lookups intentionally keep using the unfiltered method.
        CallerIs(MemberRole.OrgAdmin);
        var targetId = Guid.NewGuid();
        _members.GetByOrganizationAndUserAsync(_orgId, targetId, Arg.Any<CancellationToken>())
            .Returns(TestData.Member(_orgId, targetId, MemberRole.Member, isActive: false));

        var result = await new RemoveMemberCommandHandler(Authorization, _members, _unitOfWork)
            .Handle(new RemoveMemberCommand(_orgId, targetId), default);

        result.Error.Should().Be(OrganizationMemberErrors.AlreadyInactive);
    }

    [Fact]
    public async Task UpdateMemberRole_ByRemovedAdmin_IsDenied()
    {
        CallerIsRemovedAdmin();

        var result = await new UpdateMemberRoleCommandHandler(Authorization, _members, _unitOfWork)
            .Handle(new UpdateMemberRoleCommand(_orgId, Guid.NewGuid(), MemberRole.OrgAdmin), default);

        result.Error.Should().Be(OrganizationErrors.NotAMember);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMemberRole_OnAnInactiveTarget_IsNotFound()
    {
        CallerIs(MemberRole.OrgAdmin);
        var targetId = Guid.NewGuid();
        _members.GetByOrganizationAndUserAsync(_orgId, targetId, Arg.Any<CancellationToken>())
            .Returns(TestData.Member(_orgId, targetId, MemberRole.Member, isActive: false));

        var result = await new UpdateMemberRoleCommandHandler(Authorization, _members, _unitOfWork)
            .Handle(new UpdateMemberRoleCommand(_orgId, targetId, MemberRole.OrgAdmin), default);

        result.Error.Should().Be(OrganizationMemberErrors.NotFound);
    }

    // ---- Teams ---------------------------------------------------------------------------------

    private CreateTeamCommandHandler CreateTeamHandler() => new(
        Authorization, _members, Substitute.For<IOrganizationTeamRepository>(),
        Substitute.For<IOrganizationTeamMemberRepository>(), _unitOfWork, _clock);

    [Fact]
    public async Task CreateTeam_ByRemovedAdmin_IsDenied()
    {
        CallerIsRemovedAdmin();

        var result = await CreateTeamHandler().Handle(new CreateTeamCommand(_orgId, Guid.NewGuid(), "Team", null), default);

        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task CreateTeam_WithAnInactiveManager_IsRejected()
    {
        CallerIs(MemberRole.OrgAdmin);
        var inactiveManager = TestData.Member(_orgId, Guid.NewGuid(), MemberRole.Member, isActive: false);
        _members.GetByIdAsync(inactiveManager.Id, Arg.Any<CancellationToken>()).Returns(inactiveManager);

        var result = await CreateTeamHandler().Handle(new CreateTeamCommand(_orgId, inactiveManager.Id, "Team", null), default);

        result.Error.Should().Be(OrganizationTeamErrors.ManagerNotOrgMember);
    }

    // ---- Invitations ---------------------------------------------------------------------------

    private CreateInvitationCommandHandler CreateInvitationHandler() => new(
        Caller, Authorization, Substitute.For<IOrganizationRepository>(), _members,
        Substitute.For<IOrganizationInvitationRepository>(), Substitute.For<IUserRepository>(),
        Substitute.For<IEmailNotifier>(), new ConfigurationBuilder().Build(),
        Substitute.For<ITokenHasher>(), _unitOfWork, _clock);

    [Fact]
    public async Task CreateInvitation_ByRemovedAdmin_IsDenied()
    {
        CallerIsRemovedAdmin();

        var result = await CreateInvitationHandler().Handle(
            new CreateInvitationCommand(_orgId, "someone@example.com", MemberRole.Member), default);

        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task CreateInvitation_ByAMember_IsDeniedWithInsufficientRole()
    {
        CallerIs(MemberRole.Member);

        var result = await CreateInvitationHandler().Handle(
            new CreateInvitationCommand(_orgId, "someone@example.com", MemberRole.Member), default);

        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
    }

    private (OrganizationInvitation Invitation, IOrganizationInvitationRepository Repository) InvitationBy(Guid invitedBy)
    {
        var invitation = OrganizationInvitation.Create(
            _orgId, "someone@example.com", MemberRole.Member,
            invitedBy, "hash", TestData.UtcNow.AddDays(7), TestData.UtcNow).Value;
        var repository = Substitute.For<IOrganizationInvitationRepository>();
        repository.GetByIdAsync(invitation.Id, Arg.Any<CancellationToken>()).Returns(invitation);
        return (invitation, repository);
    }

    [Fact]
    public async Task CancelInvitation_ByInviterWhoWasRemoved_IsDenied()
    {
        // The original inviter used to bypass the membership check entirely.
        var (invitation, repository) = InvitationBy(_callerId);
        CallerIsRemovedAdmin();

        var result = await new CancelInvitationCommandHandler(Caller, repository, Authorization, _unitOfWork, _clock)
            .Handle(new CancelInvitationCommand(_orgId, invitation.Id), default);

        result.IsFailure.Should().BeTrue();
        invitation.Status.Should().Be(enInvitationStatus.Pending);
    }

    [Fact]
    public async Task CancelInvitation_ByActiveInviterWithoutAdminRole_Succeeds()
    {
        var (invitation, repository) = InvitationBy(_callerId);
        CallerIs(MemberRole.Member);

        var result = await new CancelInvitationCommandHandler(Caller, repository, Authorization, _unitOfWork, _clock)
            .Handle(new CancelInvitationCommand(_orgId, invitation.Id), default);

        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(enInvitationStatus.Cancelled);
    }

    [Fact]
    public async Task CancelInvitation_ByActiveMemberWhoIsNeitherInviterNorAdmin_IsDenied()
    {
        var (invitation, repository) = InvitationBy(Guid.NewGuid());
        CallerIs(MemberRole.Member);

        var result = await new CancelInvitationCommandHandler(Caller, repository, Authorization, _unitOfWork, _clock)
            .Handle(new CancelInvitationCommand(_orgId, invitation.Id), default);

        result.Error.Should().Be(OrganizationInvitationErrors.NotInviter);
    }

    [Fact]
    public async Task CancelInvitation_ByAnAdminWhoDidNotInvite_Succeeds()
    {
        var (invitation, repository) = InvitationBy(Guid.NewGuid());
        CallerIs(MemberRole.OrgAdmin);

        var result = await new CancelInvitationCommandHandler(Caller, repository, Authorization, _unitOfWork, _clock)
            .Handle(new CancelInvitationCommand(_orgId, invitation.Id), default);

        result.IsSuccess.Should().BeTrue();
    }
}
