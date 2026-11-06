using Shortly.Core.Members.RemoveMember;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;

namespace Shortly.Core.UnitTests.Members;

public class RemoveMemberCommandHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly AuthorizationData _authorization = new();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly IOrganizationTeamRepository _teams = Substitute.For<IOrganizationTeamRepository>();
    private readonly IOrganizationTeamMemberRepository _teamMembers = Substitute.For<IOrganizationTeamMemberRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RemoveMemberCommandHandler _sut;

    public RemoveMemberCommandHandlerTests()
    {
        _authorization.CallerIs(_orgId, _callerId, MemberRole.OrgAdmin);
        _sut = new RemoveMemberCommandHandler(_authorization.Service, _members, _teams, _teamMembers, _unitOfWork);
    }

    private OrganizationMember Target(MemberRole role, bool isActive = true)
    {
        var userId = Guid.NewGuid();
        var member = MemberData.Create(_orgId, userId, role, isActive);
        _members.GetByOrganizationAndUserAsync(_orgId, userId, Arg.Any<CancellationToken>()).Returns(member);
        return member;
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_When_TheCallerIsNotAMember()
    {
        // Arrange
        _authorization.CallerIsNotAMember(_orgId);

        // Act
        var result = await _sut.Handle(new RemoveMemberCommand(_orgId, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task Handle_Should_ReturnInsufficientRole_AndRemoveNobody_When_TheCallerIsBelowAdmin(MemberRole role)
    {
        // Arrange
        _authorization.CallerIs(_orgId, _callerId, role);
        var target = Target(MemberRole.Member);

        // Act
        var result = await _sut.Handle(new RemoveMemberCommand(_orgId, target.UserId), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        target.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheTargetIsNotInTheOrganization()
    {
        // Act
        var result = await _sut.Handle(new RemoveMemberCommand(_orgId, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.NotFound);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    [InlineData(MemberRole.OrgAdmin)]
    public async Task Handle_Should_DeactivateTheTarget_KeepingTheirRow_AndSave(MemberRole targetRole)
    {
        // Arrange
        var target = Target(targetRole);

        // Act
        var result = await _sut.Handle(new RemoveMemberCommand(_orgId, target.UserId), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        target.IsActive.Should().BeFalse();
        target.Role.Should().Be(targetRole);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnCannotRemoveOwner_AndKeepTheOwnerActive()
    {
        // Arrange
        var owner = Target(MemberRole.OrgOwner);

        // Act
        var result = await _sut.Handle(new RemoveMemberCommand(_orgId, owner.UserId), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.CannotRemoveOwner);
        owner.IsActive.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReportAlreadyInactive_When_TheTargetWasAlreadyRemoved()
    {
        // Arrange: target lookups intentionally use the unfiltered method so the row is still found
        var target = Target(MemberRole.Member, isActive: false);

        // Act
        var result = await _sut.Handle(new RemoveMemberCommand(_orgId, target.UserId), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.AlreadyInactive);
    }

    [Fact]
    public async Task Handle_Should_RemoveTheTargetsTeamMemberships()
    {
        // Arrange
        var target = Target(MemberRole.Member);
        var membership = OrganizationTeamMember.Create(Guid.NewGuid(), target.Id, TestClock.UtcNow);
        _teamMembers.GetByMemberAsync(target.Id, Arg.Any<CancellationToken>()).Returns([membership]);

        // Act
        var result = await _sut.Handle(new RemoveMemberCommand(_orgId, target.UserId), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _teamMembers.Received(1).Remove(membership);
    }

    [Fact]
    public async Task Handle_Should_ReturnMemberManagesTeams_AndChangeNothing_When_TheTargetManagesATeam()
    {
        // Arrange
        var target = Target(MemberRole.TeamManager);
        _teams.IsManagerOfAnyTeamAsync(target.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(new RemoveMemberCommand(_orgId, target.UserId), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.MemberManagesTeams);
        _teamMembers.DidNotReceiveWithAnyArgs().Remove(default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
