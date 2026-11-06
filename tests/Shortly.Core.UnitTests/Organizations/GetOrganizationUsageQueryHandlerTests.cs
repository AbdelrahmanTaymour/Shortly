using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Organizations.GetOrganizationUsage;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests.Organizations;

/// <summary>Usage figures are limited to admins and owners of the organization, and to system administrators.</summary>
public class GetOrganizationUsageQueryHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();

    private GetOrganizationUsageQueryHandler CreateSut(ISqlConnectionFactory sql, bool isAdmin = false)
    {
        var caller = CallerData.Create(_userId, isAdmin);
        return new GetOrganizationUsageQueryHandler(caller, new OrganizationAuthorizationService(caller, _members), sql);
    }

    private void CallerHasRole(MemberRole role) =>
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(MemberData.Create(_orgId, _userId, role));

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_AndNotQueryTheDatabase_When_TheCallerIsNotAMember()
    {
        // Arrange
        var sql = SqlFactoryData.ThatSignalsAccess();

        // Act
        var result = await CreateSut(sql).Handle(new GetOrganizationUsageQuery(_orgId), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        sql.DidNotReceive().CreateConnection();
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task Handle_Should_ReturnInsufficientRole_When_TheCallerIsBelowAdmin(MemberRole role)
    {
        // Arrange
        CallerHasRole(role);
        var sql = SqlFactoryData.ThatSignalsAccess();

        // Act
        var result = await CreateSut(sql).Handle(new GetOrganizationUsageQuery(_orgId), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        sql.DidNotReceive().CreateConnection();
    }

    [Theory]
    [InlineData(MemberRole.OrgAdmin)]
    [InlineData(MemberRole.OrgOwner)]
    public async Task Handle_Should_ReachTheQuery_When_TheCallerIsAnAdminOrOwner(MemberRole role)
    {
        // Arrange
        CallerHasRole(role);

        // Act
        var act = () => CreateSut(SqlFactoryData.ThatSignalsAccess()).Handle(new GetOrganizationUsageQuery(_orgId), default);

        // Assert
        await act.Should().ThrowAsync<SqlReachedException>();
    }

    [Fact]
    public async Task Handle_Should_ReachTheQuery_When_TheCallerIsASystemAdminWithoutAMembership()
    {
        // Act
        var act = () => CreateSut(SqlFactoryData.ThatSignalsAccess(), isAdmin: true)
            .Handle(new GetOrganizationUsageQuery(_orgId), default);

        // Assert
        await act.Should().ThrowAsync<SqlReachedException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_When_TheCallerIsAnAdminWhoWasRemoved()
    {
        // Arrange: only the unfiltered lookup still sees the removed admin's row
        _members.GetByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(MemberData.Create(_orgId, _userId, MemberRole.OrgAdmin, isActive: false));
        var sql = SqlFactoryData.ThatSignalsAccess();

        // Act
        var result = await CreateSut(sql).Handle(new GetOrganizationUsageQuery(_orgId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        sql.DidNotReceive().CreateConnection();
    }
}
