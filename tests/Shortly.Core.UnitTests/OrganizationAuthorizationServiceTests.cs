using Shortly.Core.Abstractions.Authentication;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests;

public class OrganizationAuthorizationServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();

    private OrganizationAuthorizationService CreateSut(Guid? claimedOrganizationId = null) =>
        new(TestData.Caller(_userId, organizationId: claimedOrganizationId), _members);

    [Fact]
    public async Task RequireMembership_WhenNotActiveMember_ReturnsNotAMember()
    {
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns((OrganizationMember?)null);

        var result = await CreateSut().RequireMembershipAsync(_orgId, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task RequireMembership_WhenAnonymous_ReturnsNotAMemberWithoutQueryingDatabase()
    {
        var anonymous = Substitute.For<IUserContext>();
        anonymous.IsAuthenticated.Returns(false);

        var result = await new OrganizationAuthorizationService(anonymous, _members).RequireMembershipAsync(_orgId, default);

        result.Error.Should().Be(OrganizationErrors.NotAMember);
        await _members.DidNotReceiveWithAnyArgs().GetActiveByOrganizationAndUserAsync(default, default);
    }

    [Fact]
    public async Task RequireMembership_UsesActiveLookupOnly()
    {
        // A deactivated (removed) member must not be treated as a member even though the row still exists.
        var removed = TestData.Member(_orgId, _userId, MemberRole.OrgAdmin, isActive: false);
        _members.GetByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns(removed);
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns((OrganizationMember?)null);

        var result = await CreateSut().RequireMembershipAsync(_orgId, default);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(MemberRole.Viewer, false)]
    [InlineData(MemberRole.Member, false)]
    [InlineData(MemberRole.TeamManager, false)]
    [InlineData(MemberRole.OrgAdmin, true)]
    [InlineData(MemberRole.OrgOwner, true)]
    public async Task RequireRole_OrgAdmin_AllowsOnlyAdminsAndOwners(MemberRole role, bool allowed)
    {
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(TestData.Member(_orgId, _userId, role));

        var result = await CreateSut().RequireRoleAsync(_orgId, MemberRole.OrgAdmin, default);

        result.IsSuccess.Should().Be(allowed);
        if (!allowed) result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
    }

    [Fact]
    public async Task RequireMembership_CachesLookupForTheRequest()
    {
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(TestData.Member(_orgId, _userId, MemberRole.Member));
        var sut = CreateSut();

        await sut.RequireMembershipAsync(_orgId, default);
        await sut.RequireMembershipAsync(_orgId, default);
        await sut.RequireRoleAsync(_orgId, MemberRole.Member, default);

        await _members.Received(1).GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveClaimed_WithoutOrganizationClaim_IsPersonalScopeAndSkipsDatabase()
    {
        var result = await CreateSut(claimedOrganizationId: null).ResolveClaimedMembershipAsync(default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(ClaimedMembership.Personal);
        result.Value.Member.Should().BeNull();
        await _members.DidNotReceiveWithAnyArgs().GetActiveByOrganizationAndUserAsync(default, default);
    }

    [Fact]
    public async Task ResolveClaimed_WhenClaimIsStale_Fails()
    {
        // Token still says "member of org", but the member was removed after the token was issued.
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns((OrganizationMember?)null);

        var result = await CreateSut(claimedOrganizationId: _orgId).ResolveClaimedMembershipAsync(default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task ResolveClaimed_WhenMembershipStillActive_ReturnsDatabaseMembership()
    {
        var member = TestData.Member(_orgId, _userId, MemberRole.Member);
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns(member);

        var result = await CreateSut(claimedOrganizationId: _orgId).ResolveClaimedMembershipAsync(default);

        result.Value.Member.Should().BeSameAs(member);
    }
}
