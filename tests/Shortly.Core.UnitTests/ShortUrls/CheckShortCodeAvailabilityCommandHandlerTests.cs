using Shortly.Core.ShortUrls.CheckShortCodeAvailability;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.UnitTests.ShortUrls;

public class CheckShortCodeAvailabilityCommandHandlerTests
{
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly CheckShortCodeAvailabilityCommandHandler _sut;

    public CheckShortCodeAvailabilityCommandHandlerTests() => _sut = new CheckShortCodeAvailabilityCommandHandler(_shortUrls);

    [Fact]
    public async Task Handle_Should_ReturnTrue_When_NoLinkUsesTheCode()
    {
        // Act
        var result = await _sut.Handle(new CheckShortCodeAvailabilityCommand("mycode1"), default);

        // Assert
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnFalse_When_ALinkAlreadyUsesTheCode()
    {
        // Arrange
        _shortUrls.ExistsByShortCodeAsync(Arg.Is<ShortCode>(c => c.Value == "mycode1"), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(new CheckShortCodeAvailabilityCommand("mycode1"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("has space")]
    public async Task Handle_Should_ReturnTheCodeError_WithoutQueryingTheRepository_When_TheCodeIsInvalid(string code)
    {
        // Act
        var result = await _sut.Handle(new CheckShortCodeAvailabilityCommand(code), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        await _shortUrls.DidNotReceiveWithAnyArgs().ExistsByShortCodeAsync(default!, default);
    }
}
