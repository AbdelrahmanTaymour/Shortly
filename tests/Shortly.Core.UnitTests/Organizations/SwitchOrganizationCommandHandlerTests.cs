using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Organizations.SwitchOrganization;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Organizations;

public class SwitchOrganizationCommandHandlerTests
{
    private static readonly AccessTokenResponse OrganizationTokens = new("org-access-token", "org-refresh-token", 900);

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly User _user = UserData.Create();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly SwitchOrganizationCommandHandler _sut;

    public SwitchOrganizationCommandHandlerTests()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _jwtService.IssueTokensAsync(Arg.Any<User>(), Arg.Any<OrganizationMember>(), Arg.Any<CancellationToken>()).Returns(OrganizationTokens);
        _sut = new SwitchOrganizationCommandHandler(CallerData.Create(_user.Id), _users, _members, _jwtService);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_AndIssueNothing_When_TheCallerHasNoActiveMembership()
    {
        // Act
        var result = await _sut.Handle(new SwitchOrganizationCommand(_orgId), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        await _jwtService.DidNotReceiveWithAnyArgs().IssueTokensAsync(default!, default(OrganizationMember)!, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_When_TheMembershipIsInactive()
    {
        // Arrange
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _user.Id, Arg.Any<CancellationToken>())
            .Returns(MemberData.Create(_orgId, _user.Id, MemberRole.Member, isActive: false));

        // Act
        var result = await _sut.Handle(new SwitchOrganizationCommand(_orgId), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task Handle_Should_IssueTokensCarryingTheMembership_When_TheCallerIsAnActiveMember()
    {
        // Arrange
        var membership = MemberData.Create(_orgId, _user.Id, MemberRole.Viewer);
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _user.Id, Arg.Any<CancellationToken>()).Returns(membership);

        // Act
        var result = await _sut.Handle(new SwitchOrganizationCommand(_orgId), default);

        // Assert
        result.Value.Should().Be(OrganizationTokens);
        await _jwtService.Received(1).IssueTokensAsync(_user, membership, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAuthenticated_When_TheCallerNoLongerExists()
    {
        // Arrange
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _user.Id, Arg.Any<CancellationToken>())
            .Returns(MemberData.Create(_orgId, _user.Id, MemberRole.Member));
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(new SwitchOrganizationCommand(_orgId), default);

        // Assert
        result.Error.Should().Be(AuthenticationErrors.NotAuthenticated);
    }
}
