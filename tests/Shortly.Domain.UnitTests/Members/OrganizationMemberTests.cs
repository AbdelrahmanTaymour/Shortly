using Shortly.Domain.Members;
using Shortly.Domain.UnitTests.TestData;

namespace Shortly.Domain.UnitTests.Members;

public class OrganizationMemberTests
{
    private static readonly DateTime Now = TestClock.Now;

    // ---- creation ----------------------------------------------------------------------------------------------------

    [Fact]
    public void Invite_Should_CreateAnActiveMember_WithTheGivenRole()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invitedBy = Guid.NewGuid();

        // Act
        var result = OrganizationMember.Invite(organizationId, userId, MemberRole.Viewer, invitedBy, Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var member = result.Value;
        member.Id.Should().NotBeEmpty();
        member.OrganizationId.Should().Be(organizationId);
        member.UserId.Should().Be(userId);
        member.Role.Should().Be(MemberRole.Viewer);
        member.InvitedBy.Should().Be(invitedBy);
        member.IsActive.Should().BeTrue();
        member.JoinedAt.Should().Be(Now);
    }

    [Fact]
    public void Invite_Should_Fail_When_TheRoleIsOwner()
    {
        // Act
        var result = OrganizationMember.Invite(Guid.NewGuid(), Guid.NewGuid(), MemberRole.OrgOwner, Guid.NewGuid(), Now);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.CannotGrantOwnerRoleDirectly);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    [InlineData(MemberRole.OrgAdmin)]
    public void Invite_Should_Succeed_ForEveryNonOwnerRole(MemberRole role)
    {
        // Act
        var result = OrganizationMember.Invite(Guid.NewGuid(), Guid.NewGuid(), role, Guid.NewGuid(), Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be(role);
    }

    [Fact]
    public void CreateOwner_Should_CreateAnActiveOwner_InvitedByThemselves()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var member = OrganizationMember.CreateOwner(organizationId, userId, Now);

        // Assert
        member.Role.Should().Be(MemberRole.OrgOwner);
        member.OrganizationId.Should().Be(organizationId);
        member.UserId.Should().Be(userId);
        member.InvitedBy.Should().Be(userId);
        member.IsActive.Should().BeTrue();
    }

    // ---- role changes ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    [InlineData(MemberRole.OrgAdmin)]
    public void ChangeRole_Should_SetTheNewRole_ForANonOwner(MemberRole newRole)
    {
        // Arrange
        var member = OrganizationData.CreateMember(MemberRole.Member);

        // Act
        var result = member.ChangeRole(newRole);

        // Assert
        result.IsSuccess.Should().BeTrue();
        member.Role.Should().Be(newRole);
    }

    [Fact]
    public void ChangeRole_Should_Fail_AndKeepTheOwnerRole_When_TheMemberIsTheOwner()
    {
        // Arrange
        var owner = OrganizationData.CreateOwner();

        // Act
        var result = owner.ChangeRole(MemberRole.Member);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.CannotChangeOwnerRole);
        owner.Role.Should().Be(MemberRole.OrgOwner);
    }

    [Fact]
    public void ChangeRole_Should_Fail_When_GrantingTheOwnerRole()
    {
        // Arrange
        var member = OrganizationData.CreateMember(MemberRole.OrgAdmin);

        // Act
        var result = member.ChangeRole(MemberRole.OrgOwner);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.CannotGrantOwnerRoleDirectly);
        member.Role.Should().Be(MemberRole.OrgAdmin);
    }

    [Fact]
    public void ChangeRole_Should_ReportTheOwnerError_First_WhenAnOwnerIsGivenTheOwnerRole()
    {
        // Arrange
        var owner = OrganizationData.CreateOwner();

        // Act
        var result = owner.ChangeRole(MemberRole.OrgOwner);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.CannotChangeOwnerRole);
    }

    [Fact]
    public void ChangeRole_Should_Succeed_WhenTheRoleIsUnchanged()
    {
        // Arrange
        var member = OrganizationData.CreateMember(MemberRole.Viewer);

        // Act
        var result = member.ChangeRole(MemberRole.Viewer);

        // Assert
        result.IsSuccess.Should().BeTrue();
        member.Role.Should().Be(MemberRole.Viewer);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void ChangeRole_Should_Fail_AndKeepTheRole_When_TheRoleIsNotDefined(int value)
    {
        // Arrange
        var member = OrganizationData.CreateMember(MemberRole.Member);

        // Act
        var result = member.ChangeRole((MemberRole)value);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InvalidRole);
        member.Role.Should().Be(MemberRole.Member);
    }

    // ---- ownership transfer ------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.OrgAdmin)]
    public void PromoteToOwner_Should_MakeTheMemberTheOwner(MemberRole before)
    {
        // Arrange
        var member = OrganizationData.CreateMember(before);

        // Act
        var result = member.PromoteToOwner();

        // Assert
        result.IsSuccess.Should().BeTrue();
        member.Role.Should().Be(MemberRole.OrgOwner);
    }

    [Fact]
    public void PromoteToOwner_Should_Fail_When_TheMemberIsInactive()
    {
        // Arrange
        var member = OrganizationData.CreateMember();
        member.Deactivate();

        // Act
        var result = member.PromoteToOwner();

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.NotActive);
        member.Role.Should().Be(MemberRole.Member);
    }

    [Fact]
    public void PromoteToOwner_Should_Fail_When_TheMemberIsAlreadyTheOwner()
    {
        // Arrange
        var owner = OrganizationData.CreateOwner();

        // Act
        var result = owner.PromoteToOwner();

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.AlreadyOwner);
    }

    [Theory]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.OrgAdmin)]
    public void DemoteFromOwner_Should_SetTheFallbackRole(MemberRole fallback)
    {
        // Arrange
        var owner = OrganizationData.CreateOwner();

        // Act
        var result = owner.DemoteFromOwner(fallback);

        // Assert
        result.IsSuccess.Should().BeTrue();
        owner.Role.Should().Be(fallback);
    }

    [Fact]
    public void DemoteFromOwner_Should_Fail_When_TheMemberIsNotTheOwner()
    {
        // Arrange
        var member = OrganizationData.CreateMember(MemberRole.OrgAdmin);

        // Act
        var result = member.DemoteFromOwner(MemberRole.Member);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.NotOwner);
        member.Role.Should().Be(MemberRole.OrgAdmin);
    }

    [Fact]
    public void DemoteFromOwner_Should_Fail_When_TheFallbackIsTheOwnerRole()
    {
        // Arrange
        var owner = OrganizationData.CreateOwner();

        // Act
        var result = owner.DemoteFromOwner(MemberRole.OrgOwner);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.CannotGrantOwnerRoleDirectly);
        owner.Role.Should().Be(MemberRole.OrgOwner);
    }

    // ---- removal and re-activation -----------------------------------------------------------------------------------

    [Fact]
    public void Deactivate_Should_MakeTheMemberInactive_AndKeepTheirRole()
    {
        // Arrange
        var member = OrganizationData.CreateMember(MemberRole.OrgAdmin);

        // Act
        var result = member.Deactivate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        member.IsActive.Should().BeFalse();
        member.Role.Should().Be(MemberRole.OrgAdmin);
    }

    [Fact]
    public void Deactivate_Should_Fail_When_TheMemberIsTheOwner()
    {
        // Arrange
        var owner = OrganizationData.CreateOwner();

        // Act
        var result = owner.Deactivate();

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.CannotRemoveOwner);
        owner.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_Should_Fail_When_AlreadyInactive()
    {
        // Arrange
        var member = OrganizationData.CreateMember();
        member.Deactivate();

        // Act
        var result = member.Deactivate();

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.AlreadyInactive);
    }

    [Fact]
    public void Reactivate_Should_MakeTheMemberActive_WithTheNewRoleAndJoinDate()
    {
        // Arrange
        var member = OrganizationData.CreateMember(MemberRole.OrgAdmin);
        member.Deactivate();
        var later = Now.AddDays(30);

        // Act
        var result = member.Reactivate(MemberRole.Viewer, later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        member.IsActive.Should().BeTrue();
        member.Role.Should().Be(MemberRole.Viewer);
        member.JoinedAt.Should().Be(later);
    }

    [Fact]
    public void Reactivate_Should_Fail_When_AlreadyActive()
    {
        // Arrange
        var member = OrganizationData.CreateMember();

        // Act
        var result = member.Reactivate(MemberRole.Viewer, Now.AddDays(1));

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.AlreadyActive);
        member.Role.Should().Be(MemberRole.Member);
        member.JoinedAt.Should().Be(Now);
    }

    [Fact]
    public void Reactivate_Should_Fail_AndStayInactive_When_TheRoleIsOwner()
    {
        // Arrange
        var member = OrganizationData.CreateMember(MemberRole.OrgAdmin);
        member.Deactivate();

        // Act
        var result = member.Reactivate(MemberRole.OrgOwner, Now.AddDays(1));

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.CannotGrantOwnerRoleDirectly);
        member.IsActive.Should().BeFalse();
        member.Role.Should().Be(MemberRole.OrgAdmin);
    }
}
