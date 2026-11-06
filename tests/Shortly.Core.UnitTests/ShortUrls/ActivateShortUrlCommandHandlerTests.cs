using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.ShortUrls.ActivateShortUrl;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.UnitTests.ShortUrls;

public class ActivateShortUrlCommandHandlerTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private Task<Result> Handle(ShortUrl? link, IUserContext caller)
    {
        _shortUrls.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(link);
        var sut = new ActivateShortUrlCommandHandler(
            _shortUrls, caller, Substitute.For<IOrganizationAuthorizationService>(), _unitOfWork, TestClock.Provider());
        return sut.Handle(new ActivateShortUrlCommand(1), default);
    }

    private static ShortUrl InactiveLinkOf(Guid ownerId)
    {
        var link = ShortUrlData.UserLink(ownerId);
        link.Deactivate(TestClock.UtcNow);
        return link;
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheLinkDoesNotExist()
    {
        // Act
        var result = await Handle(null, CallerData.Create(_ownerId));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_AndChangeNothing_When_TheLinkBelongsToSomeoneElse()
    {
        // Arrange
        var link = InactiveLinkOf(Guid.NewGuid());

        // Act
        var result = await Handle(link, CallerData.Create(_ownerId));

        // Assert: the same answer as for a missing link, so existence is not revealed
        result.Error.Should().Be(ShortUrlErrors.NotFound);
        link.IsActive.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ActivateTheLink_AndSave_When_TheCallerOwnsIt()
    {
        // Arrange
        var link = InactiveLinkOf(_ownerId);

        // Act
        var result = await Handle(link, CallerData.Create(_ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.IsActive.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ActivateTheLink_When_TheCallerIsASystemAdmin()
    {
        // Arrange
        var link = InactiveLinkOf(Guid.NewGuid());

        // Act
        var result = await Handle(link, CallerData.Create(_ownerId, isAdmin: true));

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_AndSaveNothing_When_TheLinkIsAlreadyActive()
    {
        // Arrange
        var link = ShortUrlData.UserLink(_ownerId);

        // Act
        var result = await Handle(link, CallerData.Create(_ownerId));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.AlreadyActive);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
