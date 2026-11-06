using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Organizations.ExitOrganization;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Organizations;

public class ExitOrganizationCommandHandlerTests
{
    private static readonly AccessTokenResponse PersonalTokens = new("personal-access-token", "personal-refresh-token", 900);

    private readonly User _user = UserData.Create();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly ExitOrganizationCommandHandler _sut;

    public ExitOrganizationCommandHandlerTests()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _jwtService.IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(PersonalTokens);
        _sut = new ExitOrganizationCommandHandler(CallerData.Create(_user.Id), _users, _jwtService);
    }

    [Fact]
    public async Task Handle_Should_IssueTokensForThePersonalWorkspace_WithoutAMembership()
    {
        // Act
        var result = await _sut.Handle(new ExitOrganizationCommand(), default);

        // Assert
        result.Value.Should().Be(PersonalTokens);
        await _jwtService.Received(1).IssueTokensAsync(_user, Arg.Any<CancellationToken>());
        await _jwtService.DidNotReceiveWithAnyArgs().IssueTokensAsync(default!, default(Shortly.Domain.Members.OrganizationMember)!, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAuthenticated_When_TheCallerNoLongerExists()
    {
        // Arrange
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(new ExitOrganizationCommand(), default);

        // Assert
        result.Error.Should().Be(AuthenticationErrors.NotAuthenticated);
    }
}
