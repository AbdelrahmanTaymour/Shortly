using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.ShortUrls.BulkDeactivateShortUrls;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.UnitTests.ShortUrls;

public class BulkDeactivateShortUrlsCommandHandlerTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BulkDeactivateShortUrlsCommandHandler _sut;

    public BulkDeactivateShortUrlsCommandHandlerTests() =>
        _sut = new BulkDeactivateShortUrlsCommandHandler(
            _shortUrls, CallerData.Create(_ownerId), Substitute.For<IOrganizationAuthorizationService>(),
            _unitOfWork, TestClock.Provider());

    private static ShortUrl ActiveLink(long id, Guid ownerId, string code = "abc1234") =>
        ShortUrlData.UserLink(ownerId, code).WithId(id);

    private void RepositoryHolds(params ShortUrl[] links) =>
        _shortUrls.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns(links);

    [Fact]
    public async Task Handle_Should_DeactivateEveryLinkTheCallerOwns_AndReportTheCounts()
    {
        // Arrange
        var first = ActiveLink(1, _ownerId);
        var second = ActiveLink(2, _ownerId, "other12");
        RepositoryHolds(first, second);

        // Act
        var result = await _sut.Handle(new BulkDeactivateShortUrlsCommand([1, 2]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 2, 0));
        first.IsActive.Should().BeFalse();
        second.IsActive.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SkipLinksOfOtherUsers_AndCountThemAsFailures()
    {
        // Arrange
        var own = ActiveLink(1, _ownerId);
        var foreign = ActiveLink(2, Guid.NewGuid(), "other12");
        RepositoryHolds(own, foreign);

        // Act
        var result = await _sut.Handle(new BulkDeactivateShortUrlsCommand([1, 2]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 1, 1));
        own.IsActive.Should().BeFalse();
        foreign.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_CountMissingIds_AndAlreadyInactiveLinks_AsFailures()
    {
        // Arrange: id 3 does not exist, link 2 is already inactive
        var active = ActiveLink(1, _ownerId);
        var alreadyInactive = ActiveLink(2, _ownerId, "other12");
        alreadyInactive.Deactivate(TestClock.UtcNow);
        RepositoryHolds(active, alreadyInactive);

        // Act
        var result = await _sut.Handle(new BulkDeactivateShortUrlsCommand([1, 2, 3]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(3, 1, 2));
    }
}
