using Shortly.Domain.Abstractions;

namespace Shortly.Domain.UnitTests.Abstractions;

public class ResultTests
{
    private static readonly Error SomeError = Error.Validation("Test.Error", "Something went wrong.");

    [Fact]
    public void Success_Should_BeSuccessfulWithNoError()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_Should_CarryTheError()
    {
        // Act
        var result = Result.Failure(SomeError);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SomeError);
    }

    [Fact]
    public void Failure_Should_Throw_When_ErrorIsNone()
    {
        // Act
        var act = () => Result.Failure(Error.None);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SuccessOfT_Should_ExposeTheValue()
    {
        // Act
        var result = Result.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void FailureOfT_Should_Throw_When_ValueIsAccessed()
    {
        // Arrange
        var result = Result.Failure<int>(SomeError);

        // Act
        var act = () => result.Value;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SomeError);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_Should_Succeed_When_ValueIsNotNull()
    {
        // Act
        var result = Result.Create("value");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("value");
    }

    [Fact]
    public void Create_Should_FailWithNullValueError_When_ValueIsNull()
    {
        // Act
        var result = Result.Create<string>(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NullValue);
    }

    [Fact]
    public void ImplicitConversion_Should_Succeed_When_ValueIsNotNull()
    {
        // Act
        Result<string> result = "value";

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("value");
    }

    [Fact]
    public void ImplicitConversion_Should_FailWithNullValueError_When_ValueIsNull()
    {
        // Act
        Result<string> result = (string?)null;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NullValue);
    }

    [Fact]
    public void SuccessOfT_Should_Throw_When_ValueIsNull()
    {
        // Act
        var act = () => Result.Success<string>(null!);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}
