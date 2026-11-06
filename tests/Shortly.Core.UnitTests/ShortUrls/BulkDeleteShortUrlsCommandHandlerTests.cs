using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.ShortUrls.BulkDeleteShortUrls;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.UnitTests.ShortUrls;

public class BulkDeleteShortUrlsCommandHandlerTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BulkDeleteShortUrlsCommandHandler _sut;

    public BulkDeleteShortUrlsCommandHandlerTests() =>
        _sut = new BulkDeleteShortUrlsCommandHandler(
            _shortUrls, CallerData.Create(_ownerId), Substitute.For<IOrganizationAuthorizationService>(), _unitOfWork);

    private void RepositoryHolds(params ShortUrl[] links) =>
        _shortUrls.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns(links);

    [Fact]
    public async Task Handle_Should_RemoveEveryLinkTheCallerOwns_AndReportTheCounts()
    {
        // Arrange
        var first = ShortUrlData.UserLink(_ownerId).WithId(1);
        var second = ShortUrlData.UserLink(_ownerId, "other12").WithId(2);
        RepositoryHolds(first, second);

        // Act
        var result = await _sut.Handle(new BulkDeleteShortUrlsCommand([1, 2]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 2, 0));
        _shortUrls.Received(1).Remove(first);
        _shortUrls.Received(1).Remove(second);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NeverRemoveLinksOfOtherUsers_AndCountThemAsFailures()
    {
        // Arrange
        var own = ShortUrlData.UserLink(_ownerId).WithId(1);
        var foreign = ShortUrlData.UserLink(Guid.NewGuid(), "other12").WithId(2);
        RepositoryHolds(own, foreign);

        // Act
        var result = await _sut.Handle(new BulkDeleteShortUrlsCommand([1, 2]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 1, 1));
        _shortUrls.Received(1).Remove(own);
        _shortUrls.DidNotReceive().Remove(foreign);
    }

    [Fact]
    public async Task Handle_Should_CountIdsThatDoNotExist_AsFailures()
    {
        // Arrange
        RepositoryHolds(ShortUrlData.UserLink(_ownerId).WithId(1));

        // Act
        var result = await _sut.Handle(new BulkDeleteShortUrlsCommand([1, 2, 3]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(3, 1, 2));
    }
}
