using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.ShortUrls.RemoveShortUrlPassword;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.UnitTests.ShortUrls;

public class RemoveShortUrlPasswordCommandHandlerTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private Task<Result> Handle(ShortUrl? link, IUserContext caller)
    {
        _shortUrls.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(link);
        var sut = new RemoveShortUrlPasswordCommandHandler(
            _shortUrls, caller, Substitute.For<IOrganizationAuthorizationService>(), _unitOfWork, TestClock.Provider());
        return sut.Handle(new RemoveShortUrlPasswordCommand(1), default);
    }

    private static ShortUrl ProtectedLinkOf(Guid ownerId)
    {
        var link = ShortUrlData.UserLink(ownerId);
        link.ProtectWithPassword("hash", TestClock.UtcNow);
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
    public async Task Handle_Should_ReturnNotFound_AndKeepTheProtection_When_TheLinkBelongsToSomeoneElse()
    {
        // Arrange
        var link = ProtectedLinkOf(Guid.NewGuid());

        // Act
        var result = await Handle(link, CallerData.Create(_ownerId));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
        link.IsPasswordProtected.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_RemoveThePassword_AndSave_When_TheCallerOwnsTheLink()
    {
        // Arrange
        var link = ProtectedLinkOf(_ownerId);

        // Act
        var result = await Handle(link, CallerData.Create(_ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.IsPasswordProtected.Should().BeFalse();
        link.PasswordHash.Should().BeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_AndSaveNothing_When_TheLinkIsNotProtected()
    {
        // Arrange
        var link = ShortUrlData.UserLink(_ownerId);

        // Act
        var result = await Handle(link, CallerData.Create(_ownerId));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotPasswordProtected);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
