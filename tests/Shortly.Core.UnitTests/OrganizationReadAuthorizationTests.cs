using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Members.GetMembership;
using Shortly.Core.Members.GetOrganizationMembers;
using Shortly.Core.Organizations.GetOrganizationUsage;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests;

/// <summary>
/// Regression tests: the route-supplied OrganizationId must not be treated as proof of membership.
/// A handler that reaches the (fake) SQL factory has passed authorization.
/// </summary>
public class OrganizationReadAuthorizationTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();

    private IOrganizationAuthorizationService Authorization(bool isAdmin = false) =>
        new OrganizationAuthorizationService(TestData.Caller(_userId, isAdmin), _members);

    private void CallerHasRole(MemberRole role) =>
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(TestData.Member(_orgId, _userId, role));

    // ---- GetOrganizationMembers ------------------------------------------------------------

    [Fact]
    public async Task GetMembers_ByNonMember_IsForbiddenAndDoesNotQueryMembers()
    {
        var sql = TestData.SqlFactoryThatSignalsAccess();
        var sut = new GetOrganizationMembersQueryHandler(Authorization(), sql);

        var result = await sut.Handle(new GetOrganizationMembersQuery(_orgId, 1, 20), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        sql.DidNotReceive().CreateConnection();
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.OrgOwner)]
    public async Task GetMembers_ByAnyActiveMember_ReachesQuery(MemberRole role)
    {
        CallerHasRole(role);
        var sut = new GetOrganizationMembersQueryHandler(Authorization(), TestData.SqlFactoryThatSignalsAccess());

        var act = () => sut.Handle(new GetOrganizationMembersQuery(_orgId, 1, 20), default);

        await act.Should().ThrowAsync<SqlReachedException>();
    }

    // ---- GetMembership -----------------------------------------------------------------------

    [Fact]
    public async Task GetMembership_ByNonMember_IsForbiddenAndDoesNotQueryMembers()
    {
        var sql = TestData.SqlFactoryThatSignalsAccess();
        var sut = new GetMembershipQueryHandler(Authorization(), sql);

        var result = await sut.Handle(new GetMembershipQuery(_orgId, Guid.NewGuid()), default);

        result.Error.Should().Be(OrganizationErrors.NotAMember);
        sql.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task GetMembership_ByActiveMember_ReachesQuery()
    {
        CallerHasRole(MemberRole.Member);
        var sut = new GetMembershipQueryHandler(Authorization(), TestData.SqlFactoryThatSignalsAccess());

        var act = () => sut.Handle(new GetMembershipQuery(_orgId, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<SqlReachedException>();
    }

    // ---- GetOrganizationUsage ----------------------------------------------------------------

    [Fact]
    public async Task GetUsage_ByNonMember_IsForbidden()
    {
        var sql = TestData.SqlFactoryThatSignalsAccess();
        var sut = new GetOrganizationUsageQueryHandler(TestData.Caller(_userId), Authorization(), sql);

        var result = await sut.Handle(new GetOrganizationUsageQuery(_orgId), default);

        result.Error.Should().Be(OrganizationErrors.NotAMember);
        sql.DidNotReceive().CreateConnection();
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task GetUsage_ByRegularMember_IsForbidden(MemberRole role)
    {
        CallerHasRole(role);
        var sql = TestData.SqlFactoryThatSignalsAccess();
        var sut = new GetOrganizationUsageQueryHandler(TestData.Caller(_userId), Authorization(), sql);

        var result = await sut.Handle(new GetOrganizationUsageQuery(_orgId), default);

        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        sql.DidNotReceive().CreateConnection();
    }

    [Theory]
    [InlineData(MemberRole.OrgAdmin)]
    [InlineData(MemberRole.OrgOwner)]
    public async Task GetUsage_ByAdminOrOwner_ReachesQuery(MemberRole role)
    {
        CallerHasRole(role);
        var sut = new GetOrganizationUsageQueryHandler(
            TestData.Caller(_userId), Authorization(), TestData.SqlFactoryThatSignalsAccess());

        var act = () => sut.Handle(new GetOrganizationUsageQuery(_orgId), default);

        await act.Should().ThrowAsync<SqlReachedException>();
    }

    [Fact]
    public async Task GetUsage_BySystemAdminWithoutMembership_ReachesQuery()
    {
        var admin = TestData.Caller(_userId, isAdmin: true);
        var sut = new GetOrganizationUsageQueryHandler(
            admin, new OrganizationAuthorizationService(admin, _members), TestData.SqlFactoryThatSignalsAccess());

        var act = () => sut.Handle(new GetOrganizationUsageQuery(_orgId), default);

        await act.Should().ThrowAsync<SqlReachedException>();
    }

    [Fact]
    public async Task GetUsage_ByRemovedAdmin_IsForbidden()
    {
        // Removed (inactive) admin: the active lookup returns null even though the row still exists.
        _members.GetByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(TestData.Member(_orgId, _userId, MemberRole.OrgAdmin, isActive: false));
        var sql = TestData.SqlFactoryThatSignalsAccess();
        var sut = new GetOrganizationUsageQueryHandler(TestData.Caller(_userId), Authorization(), sql);

        var result = await sut.Handle(new GetOrganizationUsageQuery(_orgId), default);

        result.IsFailure.Should().BeTrue();
        sql.DidNotReceive().CreateConnection();
    }
}
