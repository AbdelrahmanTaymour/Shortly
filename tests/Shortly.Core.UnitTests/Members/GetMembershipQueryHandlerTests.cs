using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Members.GetMembership;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests.Members;

public class GetMembershipQueryHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();

    private GetMembershipQueryHandler CreateSut(ISqlConnectionFactory sql) =>
        new(new OrganizationAuthorizationService(CallerData.Create(_userId), _members), sql);

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_AndNotQueryTheDatabase_When_TheCallerIsNotAMember()
    {
        // Arrange
        var sql = SqlFactoryData.ThatSignalsAccess();

        // Act
        var result = await CreateSut(sql).Handle(new GetMembershipQuery(_orgId, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        sql.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task Handle_Should_ReachTheQuery_When_TheCallerIsAnActiveMember()
    {
        // Arrange
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(MemberData.Create(_orgId, _userId, MemberRole.Member));

        // Act
        var act = () => CreateSut(SqlFactoryData.ThatSignalsAccess()).Handle(new GetMembershipQuery(_orgId, Guid.NewGuid()), default);

        // Assert
        await act.Should().ThrowAsync<SqlReachedException>();
    }
}
