using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Members.AddMember;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Members;

public class AddMemberCommandHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly Organization _organization;
    private readonly AuthorizationData _authorization = new();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = TestClock.Provider();
    private readonly AddMemberCommandHandler _sut;

    public AddMemberCommandHandlerTests()
    {
        _organization = OrganizationData.Create(memberLimit: 3);
        _organizations.GetByIdAsync(_orgId, Arg.Any<CancellationToken>()).Returns(_organization);
        _authorization.CallerIs(_orgId, _callerId, MemberRole.OrgAdmin);
        _users.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new AddMemberCommandHandler(
            CallerData.Create(_callerId), _organizations, _users, _authorization.Service, _members, _unitOfWork, _clock);
    }

    private static AddMemberCommand Command(Guid orgId, Guid userId, MemberRole role = MemberRole.Member) => new(orgId, userId, role);

    // ---- who may add members -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheOrganizationDoesNotExist()
    {
        // Arrange
        _organizations.GetByIdAsync(_orgId, Arg.Any<CancellationToken>()).Returns((Organization?)null);

        // Act
        var result = await _sut.Handle(Command(_orgId, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_AndAddNobody_When_TheCallerIsNotAMemberOfTheOrganization()
    {
        // Arrange
        _authorization.CallerIsNotAMember(_orgId);

        // Act
        var result = await _sut.Handle(Command(_orgId, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        _members.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task Handle_Should_ReturnInsufficientRole_When_TheCallerIsBelowAdmin(MemberRole role)
    {
        // Arrange
        _authorization.CallerIs(_orgId, _callerId, role);

        // Act
        var result = await _sut.Handle(Command(_orgId, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        _members.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Theory]
    [InlineData(MemberRole.OrgAdmin)]
    [InlineData(MemberRole.OrgOwner)]
    public async Task Handle_Should_AddTheMember_When_TheCallerIsAnAdminOrOwner(MemberRole callerRole)
    {
        // Arrange
        _authorization.CallerIs(_orgId, _callerId, callerRole);
        var newUserId = Guid.NewGuid();

        // Act
        var result = await _sut.Handle(Command(_orgId, newUserId, MemberRole.Viewer), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _members.Received(1).Add(Arg.Is<OrganizationMember>(m =>
            m.UserId == newUserId && m.OrganizationId == _orgId && m.Role == MemberRole.Viewer
            && m.IsActive && m.InvitedBy == _callerId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---- rules about the new member ----------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnCannotGrantOwnerRole_When_TheOwnerRoleIsRequested()
    {
        // Act
        var result = await _sut.Handle(Command(_orgId, Guid.NewGuid(), MemberRole.OrgOwner), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.CannotGrantOwnerRoleDirectly);
        _members.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyMember_When_TheUserIsAlreadyAnActiveMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _members.GetByOrganizationAndUserAsync(_orgId, userId, Arg.Any<CancellationToken>())
            .Returns(MemberData.Create(_orgId, userId, MemberRole.Member));

        // Act
        var result = await _sut.Handle(Command(_orgId, userId), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.AlreadyMember);
    }

    [Fact]
    public async Task Handle_Should_ReturnMemberLimitReached_When_TheOrganizationIsFull()
    {
        // Arrange
        _members.GetMemberCountByOrganizationAsync(_orgId, Arg.Any<CancellationToken>()).Returns(3);

        // Act
        var result = await _sut.Handle(Command(_orgId, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.MemberLimitReached);
        _members.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_Accept_When_TheNewMemberTakesTheLastFreeSlot()
    {
        // Arrange
        _members.GetMemberCountByOrganizationAsync(_orgId, Arg.Any<CancellationToken>()).Returns(2);

        // Act
        var result = await _sut.Handle(Command(_orgId, Guid.NewGuid()), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReactivateARemovedMember_WithTheNewRoleAndJoinDate_InsteadOfInsertingADuplicate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var removed = MemberData.Create(_orgId, userId, MemberRole.OrgAdmin, isActive: false);
        _members.GetByOrganizationAndUserAsync(_orgId, userId, Arg.Any<CancellationToken>()).Returns(removed);

        // Act
        var result = await _sut.Handle(Command(_orgId, userId, MemberRole.Viewer), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        removed.IsActive.Should().BeTrue();
        removed.Role.Should().Be(MemberRole.Viewer);
        removed.JoinedAt.Should().Be(TestClock.UtcNow);
        _members.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_NotRestoreAFormerOwnersRole_WhenTheyAreAddedAgain()
    {
        // Arrange: a previous owner who was demoted and removed
        var userId = Guid.NewGuid();
        var removed = MemberData.Create(_orgId, userId, MemberRole.Member, isActive: false);
        _members.GetByOrganizationAndUserAsync(_orgId, userId, Arg.Any<CancellationToken>()).Returns(removed);

        // Act
        await _sut.Handle(Command(_orgId, userId, MemberRole.Member), default);

        // Assert
        removed.Role.Should().Be(MemberRole.Member);
    }

    // ---- direct add --------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_MakeAnExistingUserAnActiveMemberImmediately_WithoutAnInvitation()
    {
        // Act
        var userId = Guid.NewGuid();
        var result = await _sut.Handle(Command(_orgId, userId), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _members.Received(1).Add(Arg.Is<OrganizationMember>(m => m.UserId == userId && m.IsActive));
    }

    [Fact]
    public async Task Handle_Should_ReturnUserNotFound_AndAddNobody_When_TheUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _users.ExistsAsync(userId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.Handle(Command(_orgId, userId), default);

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
        _members.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReportNotAMember_ToANonMember_EvenWhenTheOrganizationDoesNotExist()
    {
        // Arrange: authorization runs first, so a non-member cannot probe which organization ids exist
        _organizations.GetByIdAsync(_orgId, Arg.Any<CancellationToken>()).Returns((Organization?)null);
        _authorization.CallerIsNotAMember(_orgId);

        // Act
        var result = await _sut.Handle(Command(_orgId, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        await _organizations.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }
}
