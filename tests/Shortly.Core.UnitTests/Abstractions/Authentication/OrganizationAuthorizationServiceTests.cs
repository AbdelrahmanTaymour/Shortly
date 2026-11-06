using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests.Abstractions.Authentication;

public class OrganizationAuthorizationServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();

    private OrganizationAuthorizationService CreateSut(Guid? claimedOrganizationId = null) =>
        new(CallerData.Create(_userId, organizationId: claimedOrganizationId), _members);

    private void ActiveMemberHasRole(MemberRole role) =>
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(MemberData.Create(_orgId, _userId, role));

    // ---- RequireMembershipAsync --------------------------------------------------------------------------------------

    [Fact]
    public async Task RequireMembershipAsync_Should_ReturnNotAMember_When_TheCallerIsNotAnActiveMember()
    {
        // Arrange
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns((OrganizationMember?)null);

        // Act
        var result = await CreateSut().RequireMembershipAsync(_orgId, default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task RequireMembershipAsync_Should_ReturnNotAMember_WithoutQueryingTheDatabase_When_TheCallerIsAnonymous()
    {
        // Arrange
        var anonymous = Substitute.For<IUserContext>();
        anonymous.IsAuthenticated.Returns(false);

        // Act
        var result = await new OrganizationAuthorizationService(anonymous, _members).RequireMembershipAsync(_orgId, default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        await _members.DidNotReceiveWithAnyArgs().GetActiveByOrganizationAndUserAsync(default, default);
    }

    [Fact]
    public async Task RequireMembershipAsync_Should_IgnoreARemovedMember_EvenThoughTheRowStillExists()
    {
        // Arrange
        var removed = MemberData.Create(_orgId, _userId, MemberRole.OrgAdmin, isActive: false);
        _members.GetByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns(removed);
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns((OrganizationMember?)null);

        // Act
        var result = await CreateSut().RequireMembershipAsync(_orgId, default);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task RequireMembershipAsync_Should_ReturnTheDatabaseMembership_ForAnActiveMember()
    {
        // Arrange
        var member = MemberData.Create(_orgId, _userId, MemberRole.Viewer);
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns(member);

        // Act
        var result = await CreateSut().RequireMembershipAsync(_orgId, default);

        // Assert
        result.Value.Should().BeSameAs(member);
    }

    [Fact]
    public async Task RequireMembershipAsync_Should_LookTheMembershipUpOnlyOncePerRequest()
    {
        // Arrange
        ActiveMemberHasRole(MemberRole.Member);
        var sut = CreateSut();

        // Act
        await sut.RequireMembershipAsync(_orgId, default);
        await sut.RequireMembershipAsync(_orgId, default);
        await sut.RequireRoleAsync(_orgId, MemberRole.Member, default);

        // Assert
        await _members.Received(1).GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>());
    }

    // ---- RequireRoleAsync --------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(MemberRole.Viewer, false)]
    [InlineData(MemberRole.Member, false)]
    [InlineData(MemberRole.TeamManager, false)]
    [InlineData(MemberRole.OrgAdmin, true)]
    [InlineData(MemberRole.OrgOwner, true)]
    public async Task RequireRoleAsync_Should_AllowOnlyAdminsAndOwners_WhenOrgAdminIsRequired(MemberRole role, bool allowed)
    {
        // Arrange
        ActiveMemberHasRole(role);

        // Act
        var result = await CreateSut().RequireRoleAsync(_orgId, MemberRole.OrgAdmin, default);

        // Assert
        result.IsSuccess.Should().Be(allowed);
        if (!allowed) result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
    }

    [Theory]
    [InlineData(MemberRole.Viewer, MemberRole.Viewer, true)]
    [InlineData(MemberRole.Member, MemberRole.TeamManager, false)]
    [InlineData(MemberRole.OrgOwner, MemberRole.OrgOwner, true)]
    public async Task RequireRoleAsync_Should_CompareTheCallersRoleWithTheMinimum(MemberRole role, MemberRole minimum, bool allowed)
    {
        // Arrange
        ActiveMemberHasRole(role);

        // Act
        var result = await CreateSut().RequireRoleAsync(_orgId, minimum, default);

        // Assert
        result.IsSuccess.Should().Be(allowed);
    }

    [Fact]
    public async Task RequireRoleAsync_Should_ReturnNotAMember_BeforeComparingRoles_When_TheCallerIsNotAMember()
    {
        // Arrange
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns((OrganizationMember?)null);

        // Act
        var result = await CreateSut().RequireRoleAsync(_orgId, MemberRole.Viewer, default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    // ---- ResolveClaimedMembershipAsync -------------------------------------------------------------------------------

    [Fact]
    public async Task ResolveClaimedMembershipAsync_Should_ReturnThePersonalWorkspace_WithoutQueryingTheDatabase_When_NoOrganizationIsClaimed()
    {
        // Act
        var result = await CreateSut(claimedOrganizationId: null).ResolveClaimedMembershipAsync(default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(ClaimedMembership.Personal);
        result.Value.Member.Should().BeNull();
        await _members.DidNotReceiveWithAnyArgs().GetActiveByOrganizationAndUserAsync(default, default);
    }

    [Fact]
    public async Task ResolveClaimedMembershipAsync_Should_ReturnNotAMember_When_TheClaimIsStale()
    {
        // Arrange: the token still says "member of org", but the member was removed after it was issued
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns((OrganizationMember?)null);

        // Act
        var result = await CreateSut(claimedOrganizationId: _orgId).ResolveClaimedMembershipAsync(default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task ResolveClaimedMembershipAsync_Should_ReturnTheDatabaseMembership_When_ItIsStillActive()
    {
        // Arrange
        var member = MemberData.Create(_orgId, _userId, MemberRole.Member);
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns(member);

        // Act
        var result = await CreateSut(claimedOrganizationId: _orgId).ResolveClaimedMembershipAsync(default);

        // Assert
        result.Value.Member.Should().BeSameAs(member);
    }
}
