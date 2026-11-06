using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Admin.CreateUser;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Admin;

public class CreateUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly AdminCreateUserCommandHandler _sut;

    public CreateUserCommandHandlerTests()
    {
        _hasher.Hash("secret1").Returns("hashed");
        _sut = new AdminCreateUserCommandHandler(_users, _unitOfWork, TestClock.Provider(), _hasher);
    }

    private static CreateUserCommand Command(UserRole role = UserRole.User, string email = "jane@example.com") =>
        new(email, "jane", "secret1", role);

    [Fact]
    public async Task Handle_Should_ReturnNotUnique_AndCreateNothing_When_TheEmailOrUsernameIsTaken()
    {
        // Arrange
        _users.EmailOrUsernameExistsAsync("jane@example.com", "jane", Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.Error.Should().Be(UserErrors.EmailOrUsernameNotUnique);
        _users.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateAConfirmedUser_WithTheHashedPassword_AndReturnTheId()
    {
        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _users.Received(1).Add(Arg.Is<User>(u =>
            u.Id == result.Value && u.Email == "jane@example.com" && u.Username == "jane" && u.PasswordHash == "hashed"
            && u.IsEmailConfirmed && u.Role == UserRole.User && u.IsActive));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(UserRole.SuperAdmin)]
    [InlineData(UserRole.SystemAdmin)]
    public async Task Handle_Should_AssignTheRequestedRole(UserRole role)
    {
        // Act
        await _sut.Handle(Command(role), default);

        // Assert
        _users.Received(1).Add(Arg.Is<User>(u => u.Role == role));
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_AndSaveNothing_When_TheEmailIsBlank()
    {
        // Act
        var result = await _sut.Handle(Command(email: "  "), default);

        // Assert
        result.Error.Should().Be(UserErrors.EmailRequired);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
