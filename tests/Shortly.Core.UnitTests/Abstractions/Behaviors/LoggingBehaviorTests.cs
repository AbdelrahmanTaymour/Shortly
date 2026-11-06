using MediatR;
using Microsoft.Extensions.Logging;
using Shortly.Core.Abstractions.Behaviors;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.UnitTests.Abstractions.Behaviors;

public class LoggingBehaviorTests
{
    private sealed record SampleCommand : ICommand;

    private readonly CapturingLogger<LoggingBehavior<SampleCommand, Result>> _logger = new();
    private readonly LoggingBehavior<SampleCommand, Result> _sut;

    public LoggingBehaviorTests() => _sut = new LoggingBehavior<SampleCommand, Result>(_logger);

    [Fact]
    public async Task Handle_Should_ReturnTheResultOfTheNextHandler_AndLogSuccess()
    {
        // Arrange
        RequestHandlerDelegate<Result> next = _ => Task.FromResult(Result.Success());

        // Act
        var result = await _sut.Handle(new SampleCommand(), next, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _logger.Entries.Select(e => e.Level).Should().Equal(LogLevel.Information, LogLevel.Information);
        _logger.Entries[0].Message.Should().Contain("SampleCommand");
        _logger.Entries[1].Message.Should().Contain("processed successfully");
    }

    [Fact]
    public async Task Handle_Should_ReturnTheFailureUnchanged_AndLogAnError()
    {
        // Arrange
        var error = Error.Validation("Test.Failed", "It failed.");
        RequestHandlerDelegate<Result> next = _ => Task.FromResult(Result.Failure(error));

        // Act
        var result = await _sut.Handle(new SampleCommand(), next, default);

        // Assert
        result.Error.Should().Be(error);
        _logger.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message.Contains("processed with error"));
    }

    [Fact]
    public async Task Handle_Should_LogTheExceptionAndRethrowIt()
    {
        // Arrange
        var failure = new InvalidOperationException("boom");
        RequestHandlerDelegate<Result> next = _ => throw failure;

        // Act
        var act = () => _sut.Handle(new SampleCommand(), next, default);

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
        _logger.Entries.Should().Contain(e => e.Level == LogLevel.Error && ReferenceEquals(e.Exception, failure));
    }
}
