using Shortly.Core.Members.UpdateMemberRole;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests.Members;

public class UpdateMemberRoleCommandHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly AuthorizationData _authorization = new();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateMemberRoleCommandHandler _sut;

    public UpdateMemberRoleCommandHandlerTests()
    {
        _authorization.CallerIs(_orgId, _callerId, MemberRole.OrgAdmin);
        _sut = new UpdateMemberRoleCommandHandler(_authorization.Service, _members, _unitOfWork);
    }

    private OrganizationMember Target(MemberRole role, bool isActive = true)
    {
        var userId = Guid.NewGuid();
        var member = MemberData.Create(_orgId, userId, role, isActive);
        _members.GetByOrganizationAndUserAsync(_orgId, userId, Arg.Any<CancellationToken>()).Returns(member);
        return member;
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_AndSaveNothing_When_TheCallerIsNotAMember()
    {
        // Arrange
        _authorization.CallerIsNotAMember(_orgId);

        // Act
        var result = await _sut.Handle(new UpdateMemberRoleCommand(_orgId, Guid.NewGuid(), MemberRole.OrgAdmin), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task Handle_Should_ReturnInsufficientRole_AndChangeNothing_When_TheCallerIsBelowAdmin(MemberRole role)
    {
        // Arrange
        _authorization.CallerIs(_orgId, _callerId, role);
        var target = Target(MemberRole.Member);

        // Act
        var result = await _sut.Handle(new UpdateMemberRoleCommand(_orgId, target.UserId, MemberRole.OrgAdmin), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        target.Role.Should().Be(MemberRole.Member);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheTargetIsNotInTheOrganization()
    {
        // Act
        var result = await _sut.Handle(new UpdateMemberRoleCommand(_orgId, Guid.NewGuid(), MemberRole.Viewer), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheTargetWasRemoved()
    {
        // Arrange
        var target = Target(MemberRole.Member, isActive: false);

        // Act
        var result = await _sut.Handle(new UpdateMemberRoleCommand(_orgId, target.UserId, MemberRole.OrgAdmin), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.NotFound);
        target.Role.Should().Be(MemberRole.Member);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    [InlineData(MemberRole.OrgAdmin)]
    public async Task Handle_Should_ChangeTheRole_AndSave(MemberRole newRole)
    {
        // Arrange
        var target = Target(MemberRole.Member);

        // Act
        var result = await _sut.Handle(new UpdateMemberRoleCommand(_orgId, target.UserId, newRole), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        target.Role.Should().Be(newRole);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_LetAnAdminChangeAnotherAdminsRole()
    {
        // Arrange
        var otherAdmin = Target(MemberRole.OrgAdmin);

        // Act
        var result = await _sut.Handle(new UpdateMemberRoleCommand(_orgId, otherAdmin.UserId, MemberRole.Viewer), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        otherAdmin.Role.Should().Be(MemberRole.Viewer);
    }

    [Fact]
    public async Task Handle_Should_ReturnCannotChangeOwnerRole_AndKeepTheOwner()
    {
        // Arrange
        var owner = Target(MemberRole.OrgOwner);

        // Act
        var result = await _sut.Handle(new UpdateMemberRoleCommand(_orgId, owner.UserId, MemberRole.Member), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.CannotChangeOwnerRole);
        owner.Role.Should().Be(MemberRole.OrgOwner);
    }

    [Fact]
    public async Task Handle_Should_ReturnCannotGrantOwnerRole_When_TheNewRoleIsOwner()
    {
        // Arrange
        var target = Target(MemberRole.OrgAdmin);

        // Act
        var result = await _sut.Handle(new UpdateMemberRoleCommand(_orgId, target.UserId, MemberRole.OrgOwner), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.CannotGrantOwnerRoleDirectly);
        target.Role.Should().Be(MemberRole.OrgAdmin);
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidRole_When_TheRoleIsNotDefined()
    {
        // Arrange
        var target = Target(MemberRole.Member);

        // Act
        var result = await _sut.Handle(new UpdateMemberRoleCommand(_orgId, target.UserId, (MemberRole)99), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InvalidRole);
    }
}
