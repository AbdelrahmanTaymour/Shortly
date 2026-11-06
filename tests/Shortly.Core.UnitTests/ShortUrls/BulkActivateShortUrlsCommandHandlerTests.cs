using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.ShortUrls.BulkActivateShortUrls;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.UnitTests.ShortUrls;

public class BulkActivateShortUrlsCommandHandlerTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BulkActivateShortUrlsCommandHandler _sut;

    public BulkActivateShortUrlsCommandHandlerTests() =>
        _sut = new BulkActivateShortUrlsCommandHandler(
            _shortUrls, CallerData.Create(_ownerId), Substitute.For<IOrganizationAuthorizationService>(),
            _unitOfWork, TestClock.Provider());

    private ShortUrl InactiveLink(long id, Guid ownerId)
    {
        var link = ShortUrlData.UserLink(ownerId).WithId(id);
        link.Deactivate(TestClock.UtcNow);
        return link;
    }

    private void RepositoryHolds(params ShortUrl[] links) =>
        _shortUrls.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns(links);

    [Fact]
    public async Task Handle_Should_ActivateEveryLinkTheCallerOwns_AndReportTheCounts()
    {
        // Arrange
        var first = InactiveLink(1, _ownerId);
        var second = InactiveLink(2, _ownerId);
        RepositoryHolds(first, second);

        // Act
        var result = await _sut.Handle(new BulkActivateShortUrlsCommand([1, 2]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 2, 0));
        first.IsActive.Should().BeTrue();
        second.IsActive.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SkipLinksOfOtherUsers_AndCountThemAsFailures()
    {
        // Arrange
        var own = InactiveLink(1, _ownerId);
        var foreign = InactiveLink(2, Guid.NewGuid());
        RepositoryHolds(own, foreign);

        // Act
        var result = await _sut.Handle(new BulkActivateShortUrlsCommand([1, 2]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 1, 1));
        own.IsActive.Should().BeTrue();
        foreign.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_CountMissingIds_AndAlreadyActiveLinks_AsFailures()
    {
        // Arrange: id 3 does not exist, link 2 is already active
        var inactive = InactiveLink(1, _ownerId);
        var alreadyActive = ShortUrlData.UserLink(_ownerId, "other12").WithId(2);
        RepositoryHolds(inactive, alreadyActive);

        // Act
        var result = await _sut.Handle(new BulkActivateShortUrlsCommand([1, 2, 3]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(3, 1, 2));
    }
}
