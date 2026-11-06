using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.ShortUrls.BulkUpdateExpiration;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.UnitTests.ShortUrls;

public class BulkUpdateExpirationCommandHandlerTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BulkUpdateExpirationCommandHandler _sut;

    public BulkUpdateExpirationCommandHandlerTests() =>
        _sut = new BulkUpdateExpirationCommandHandler(
            _shortUrls, CallerData.Create(_ownerId), Substitute.For<IOrganizationAuthorizationService>(),
            _unitOfWork, TestClock.Provider());

    private void RepositoryHolds(params ShortUrl[] links) =>
        _shortUrls.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns(links);

    [Fact]
    public async Task Handle_Should_SetTheExpirationOfEveryLinkTheCallerOwns_AndSaveOnce()
    {
        // Arrange
        var first = ShortUrlData.UserLink(_ownerId).WithId(1);
        var second = ShortUrlData.UserLink(_ownerId, "other12").WithId(2);
        RepositoryHolds(first, second);
        var expiresAt = TestClock.UtcNow.AddDays(10);

        // Act
        var result = await _sut.Handle(new BulkUpdateExpirationCommand([1, 2], expiresAt), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 2, 0));
        first.ExpiresAt.Should().Be(expiresAt);
        second.ExpiresAt.Should().Be(expiresAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ClearTheExpiration_When_NullIsSent()
    {
        // Arrange
        var link = ShortUrl.CreateForUser(_ownerId, ShortUrlData.UserLink(_ownerId).OriginalUrl,
            ShortCode.Create("abc1234").Value, TestClock.UtcNow, expiresAt: TestClock.UtcNow.AddDays(1)).Value.WithId(1);
        RepositoryHolds(link);

        // Act
        var result = await _sut.Handle(new BulkUpdateExpirationCommand([1], null), default);

        // Assert
        result.Value.SuccessCount.Should().Be(1);
        link.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_SkipLinksOfOtherUsers_AndMissingIds_CountingThemAsFailures()
    {
        // Arrange
        var own = ShortUrlData.UserLink(_ownerId).WithId(1);
        var foreign = ShortUrlData.UserLink(Guid.NewGuid(), "other12").WithId(2);
        RepositoryHolds(own, foreign);
        var expiresAt = TestClock.UtcNow.AddDays(10);

        // Act
        var result = await _sut.Handle(new BulkUpdateExpirationCommand([1, 2, 3], expiresAt), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(3, 1, 2));
        foreign.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_CountAsFailures_AndNotSave_When_TheExpirationIsNotInTheFuture()
    {
        // Arrange
        RepositoryHolds(ShortUrlData.UserLink(_ownerId).WithId(1));

        // Act
        var result = await _sut.Handle(new BulkUpdateExpirationCommand([1], TestClock.UtcNow.AddMinutes(-1)), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(1, 0, 1));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
