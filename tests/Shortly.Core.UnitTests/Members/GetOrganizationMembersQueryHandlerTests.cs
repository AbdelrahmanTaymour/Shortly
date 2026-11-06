using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Members.GetOrganizationMembers;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests.Members;

/// <summary>
/// The route-supplied organization id is not proof of membership. A handler that reaches the (fake) SQL factory has
/// passed authorization.
/// </summary>
public class GetOrganizationMembersQueryHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();

    private GetOrganizationMembersQueryHandler CreateSut(ISqlConnectionFactory sql) =>
        new(new OrganizationAuthorizationService(CallerData.Create(_userId), _members), sql);

    private void CallerHasRole(MemberRole role) =>
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(MemberData.Create(_orgId, _userId, role));

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_AndNotQueryTheDatabase_When_TheCallerIsNotAMember()
    {
        // Arrange
        var sql = SqlFactoryData.ThatSignalsAccess();

        // Act
        var result = await CreateSut(sql).Handle(new GetOrganizationMembersQuery(_orgId, 1, 20), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        sql.DidNotReceive().CreateConnection();
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.OrgOwner)]
    public async Task Handle_Should_ReachTheQuery_When_TheCallerIsAnActiveMemberOfAnyRole(MemberRole role)
    {
        // Arrange
        CallerHasRole(role);

        // Act
        var act = () => CreateSut(SqlFactoryData.ThatSignalsAccess()).Handle(new GetOrganizationMembersQuery(_orgId, 1, 20), default);

        // Assert
        await act.Should().ThrowAsync<SqlReachedException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_When_TheCallerWasRemovedFromTheOrganization()
    {
        // Arrange: only the unfiltered lookup still sees the removed member's row
        _members.GetByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(MemberData.Create(_orgId, _userId, MemberRole.OrgAdmin, isActive: false));
        var sql = SqlFactoryData.ThatSignalsAccess();

        // Act
        var result = await CreateSut(sql).Handle(new GetOrganizationMembersQuery(_orgId, 1, 20), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        sql.DidNotReceive().CreateConnection();
    }
}
