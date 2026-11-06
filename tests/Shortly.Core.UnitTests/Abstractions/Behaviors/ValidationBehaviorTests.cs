using FluentValidation;
using MediatR;
using Shortly.Core.Abstractions.Behaviors;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using ValidationException = Shortly.Core.Exceptions.ValidationException;

namespace Shortly.Core.UnitTests.Abstractions.Behaviors;

public class ValidationBehaviorTests
{
    private sealed record SampleCommand(string Name, int Age) : ICommand;

    private sealed class NameValidator : AbstractValidator<SampleCommand>
    {
        public NameValidator() => RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
    }

    private sealed class AgeValidator : AbstractValidator<SampleCommand>
    {
        public AgeValidator() => RuleFor(x => x.Age).GreaterThan(0).WithMessage("Age must be positive.");
    }

    private bool _nextWasCalled;

    private RequestHandlerDelegate<Result> Next => _ =>
    {
        _nextWasCalled = true;
        return Task.FromResult(Result.Success());
    };

    private static ValidationBehavior<SampleCommand, Result> CreateSut(params IValidator<SampleCommand>[] validators) =>
        new(validators);

    [Fact]
    public async Task Handle_Should_CallTheNextHandler_When_ThereAreNoValidators()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new SampleCommand("", 0), Next, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _nextWasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_CallTheNextHandler_When_TheRequestIsValid()
    {
        // Arrange
        var sut = CreateSut(new NameValidator(), new AgeValidator());

        // Act
        var result = await sut.Handle(new SampleCommand("Jane", 30), Next, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _nextWasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ThrowAValidationException_AndSkipTheNextHandler_When_TheRequestIsInvalid()
    {
        // Arrange
        var sut = CreateSut(new NameValidator());

        // Act
        var act = () => sut.Handle(new SampleCommand("", 30), Next, default);

        // Assert
        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors.Should().ContainSingle()
            .Which.Should().Match<Shortly.Core.Exceptions.ValidationError>(e =>
                e.PropertyName == "Name" && e.ErrorMessage == "Name is required.");
        _nextWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_CollectTheFailuresOfEveryValidator_WithoutRepeatingAny()
    {
        // Arrange: both validators fail for this request
        var sut = CreateSut(new NameValidator(), new AgeValidator());

        // Act
        var act = () => sut.Handle(new SampleCommand("", 0), Next, default);

        // Assert
        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors.Select(e => e.PropertyName).Should().Equal("Name", "Age");
    }

    [Fact]
    public async Task Handle_Should_ReportOnlyTheValidatorsThatFailed()
    {
        // Arrange
        var sut = CreateSut(new NameValidator(), new AgeValidator());

        // Act
        var act = () => sut.Handle(new SampleCommand("Jane", 0), Next, default);

        // Assert
        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Age");
    }
}
