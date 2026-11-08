using Microsoft.EntityFrameworkCore;
using Shortly.Core.Admin.CreateUser;
using Shortly.Core.Exceptions;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Admin;

public class CreateUserTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private static CreateUserCommand Command(string? email = null, string? username = null, UserRole role = UserRole.User)
    {
        var unique = Guid.NewGuid().ToString("N");
        return new CreateUserCommand(email ?? $"{unique}@example.com", username ?? $"user{unique}", "Str0ng-password!", role);
    }

    // ---- success ----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task CreateUser_Should_CreateAnAlreadyConfirmedUser_WithTheGivenRole()
    {
        // Arrange
        var command = Command(role: UserRole.SystemAdmin);
        As(Caller.Admin());

        // Act
        var result = await Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var user = await Query(db => db.Set<User>().SingleAsync(u => u.Id == result.Value));
        user.Email.Should().Be(command.Email);
        user.Username.Should().Be(command.Username);
        user.Role.Should().Be(UserRole.SystemAdmin);
        user.IsEmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_Should_DefaultToTheUserRole_WhenNoneIsNeeded()
    {
        // Arrange
        var command = Command();
        As(Caller.Admin());

        // Act
        var result = await Send(command);

        // Assert
        (await Query(db => db.Set<User>().Where(u => u.Id == result.Value).Select(u => u.Role).SingleAsync()))
            .Should().Be(UserRole.User);
    }

    // ---- duplicates ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task CreateUser_Should_RejectADuplicateEmailOrUsername()
    {
        // Arrange
        var existing = await Data.UserAsync();
        As(Caller.Admin());

        // Act
        var result = await Send(Command(email: existing.Email));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.EmailOrUsernameNotUnique);
    }

    // ---- validation ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task CreateUser_Should_RejectInvalidInput_BeforeStoringAnything()
    {
        // Arrange
        As(Caller.Admin());

        // Act
        var act = () => Send(new CreateUserCommand("not-an-email", "u", "short", UserRole.User));

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}