using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.ValidateResetToken;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Tokens;

namespace Shortly.Core.UnitTests.Account;

public class ValidateResetTokenCommandHandlerTests
{
    private static readonly ValidateResetTokenCommand Command = new("plain-token");

    private readonly IUserActionTokenRepository _tokens = Substitute.For<IUserActionTokenRepository>();
    private readonly ValidateResetTokenCommandHandler _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public ValidateResetTokenCommandHandlerTests()
    {
        var tokenHasher = Substitute.For<ITokenHasher>();
        tokenHasher.Hash(Command.Token).Returns(TokenData.Hash);
        _sut = new ValidateResetTokenCommandHandler(_tokens, tokenHasher, TestClock.Provider());
    }

    private void TokenInTheRepository(UserActionToken? token) =>
        _tokens.GetByTokenHashAsync(TokenData.Hash, Arg.Any<CancellationToken>()).Returns(token);

    [Fact]
    public async Task Handle_Should_ReturnTrue_ForAnUnusedUnexpiredPasswordResetToken()
    {
        // Arrange
        TokenInTheRepository(TokenData.PasswordReset(_userId));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnFalse_WithoutAnError_When_NoTokenMatches()
    {
        // Arrange
        TokenInTheRepository(null);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnFalse_When_TheTokenIsNotAPasswordResetToken()
    {
        // Arrange
        TokenInTheRepository(TokenData.EmailVerification(_userId));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnFalse_When_TheTokenWasAlreadyUsed()
    {
        // Arrange
        TokenInTheRepository(TokenData.Used(TokenData.PasswordReset(_userId)));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Value.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task Handle_Should_TreatTheExactExpiryInstantAsExpired(int secondsUntilExpiry, bool expected)
    {
        // Arrange: a one-hour token that expires "secondsUntilExpiry" after the fixed clock
        var issuedAt = TestClock.UtcNow.AddHours(-1).AddSeconds(secondsUntilExpiry);
        TokenInTheRepository(TokenData.PasswordReset(_userId, issuedAt));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Value.Should().Be(expected);
    }
}
