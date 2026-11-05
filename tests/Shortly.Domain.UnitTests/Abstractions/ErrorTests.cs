using Shortly.Domain.Abstractions;

namespace Shortly.Domain.UnitTests.Abstractions;

public class ErrorTests
{
    [Fact]
    public void None_Should_HaveEmptyCodeAndMessage()
    {
        // Assert
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }

    [Fact]
    public void NullValue_Should_BeAFailureError()
    {
        // Assert
        Error.NullValue.Code.Should().Be("Error.NullValue");
        Error.NullValue.Type.Should().Be(ErrorType.Failure);
    }

    [Theory]
    [MemberData(nameof(Factories))]
    public void Factory_Should_CreateErrorOfTheMatchingType(Func<string, string, Error> factory, ErrorType expectedType)
    {
        // Act
        var error = factory("Some.Code", "Some message.");

        // Assert
        error.Code.Should().Be("Some.Code");
        error.Message.Should().Be("Some message.");
        error.Type.Should().Be(expectedType);
    }

    [Fact]
    public void Errors_Should_BeEqual_When_AllValuesMatch()
    {
        // Arrange
        var first = Error.NotFound("Code", "Message");
        var second = Error.NotFound("Code", "Message");

        // Assert
        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Errors_Should_NotBeEqual_When_TypeDiffers()
    {
        // Arrange
        var notFound = Error.NotFound("Code", "Message");
        var conflict = Error.Conflict("Code", "Message");

        // Assert
        notFound.Should().NotBe(conflict);
    }

    public static IEnumerable<object[]> Factories()
    {
        yield return [(Func<string, string, Error>)Error.Failure, ErrorType.Failure];
        yield return [(Func<string, string, Error>)Error.Validation, ErrorType.Validation];
        yield return [(Func<string, string, Error>)Error.NotFound, ErrorType.NotFound];
        yield return [(Func<string, string, Error>)Error.Conflict, ErrorType.Conflict];
        yield return [(Func<string, string, Error>)Error.Unauthorized, ErrorType.Unauthorized];
        yield return [(Func<string, string, Error>)Error.Forbidden, ErrorType.Forbidden];
    }
}
