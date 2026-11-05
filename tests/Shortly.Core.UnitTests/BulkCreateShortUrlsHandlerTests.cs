using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Cache;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Exceptions;
using Shortly.Core.ShortUrls;
using Shortly.Core.ShortUrls.BulkCreateShortUrls;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests;

public class BulkCreateShortUrlsHandlerTests
{
    private static readonly DateTime Now = TestData.UtcNow;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly IShortUrlRepository _links = Substitute.For<IShortUrlRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ISubscriptionPlanLimitsProvider _plans = Substitute.For<ISubscriptionPlanLimitsProvider>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IUserContext _caller;

    public BulkCreateShortUrlsHandlerTests()
    {
        _clock.UtcNow.Returns(Now);
        _links.GetExistingShortCodesAsync(Arg.Any<IReadOnlyCollection<ShortCode>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());
        _caller = TestData.Caller(_userId);
        _caller.OwnerType.Returns(enShortUrlOwnerType.User);
        _caller.IpAddress.Returns("203.0.113.1");
    }

    private User GivenUser(int maxLinks, bool bulk, int usedThisMonth)
    {
        var user = User.Register($"{Guid.NewGuid():N}@example.com", "name", "hash", Now).Value;
        for (var i = 0; i < usedThisMonth; i++) user.Usage.IncrementLinksCreated(Now);
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _plans.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>())
            .Returns(new SubscriptionPlanLimits(maxLinks, 0, 0, false, bulk, false, false, false, false));
        return user;
    }

    private BulkCreateShortUrlsCommandHandler Sut() => new(
        _links, _caller,
        new ShortUrlOwnerResolver(_caller, _users, Substitute.For<IOrganizationRepository>(),
            Substitute.For<IOrganizationAuthorizationService>(), _plans),
        _unitOfWork, new ShortCodeGenerator(), _clock);

    private static BulkCreateShortUrlsCommand Command(int valid, int invalid = 0) => new(
        Enumerable.Range(0, valid).Select(i => new BulkCreateShortUrlItem($"https://example.com/{i}", null, null))
            .Concat(Enumerable.Range(0, invalid).Select(_ => new BulkCreateShortUrlItem("not a url", null, null)))
            .ToList());

    [Fact]
    public async Task PlanWithoutBulkCreation_IsRejectedBeforeAnythingIsCreated()
    {
        GivenUser(maxLinks: 100, bulk: false, usedThisMonth: 0);

        var result = await Sut().Handle(Command(valid: 3), default);

        result.Error.Should().Be(ShortUrlErrors.BulkCreationNotAllowed);
        _links.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidItemsWithinQuota_AreAllCreated_AndUsageIncreasesByTheirCount()
    {
        var user = GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 10);

        var result = await Sut().Handle(Command(valid: 4), default);

        result.Value.SuccessCount.Should().Be(4);
        result.Value.FailureCount.Should().Be(0);
        _links.Received(4).Add(Arg.Any<ShortUrl>());
        user.Usage.MonthlyLinksCreated.Should().Be(14);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidItemsExceedingRemainingQuota_RejectTheWholeRequest()
    {
        var user = GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 498);

        var result = await Sut().Handle(Command(valid: 3), default);

        result.Error.Should().Be(ShortUrlErrors.MonthlyLinkLimitReached);
        _links.DidNotReceiveWithAnyArgs().Add(default!);
        user.Usage.MonthlyLinksCreated.Should().Be(498);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidItemsExactlyFillingTheQuota_AreAccepted()
    {
        var user = GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 497);

        var result = await Sut().Handle(Command(valid: 3), default);

        result.Value.SuccessCount.Should().Be(3);
        user.Usage.MonthlyLinksCreated.Should().Be(500);
    }

    [Fact]
    public async Task OnlyValidItemsCountAgainstTheQuota_InvalidItemsStillReportedAsFailures()
    {
        var user = GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 497);

        // 3 valid items fit the remaining 3, even though the request has 5 items in total.
        var result = await Sut().Handle(Command(valid: 3, invalid: 2), default);

        result.Value.TotalProcessed.Should().Be(5);
        result.Value.SuccessCount.Should().Be(3);
        result.Value.FailureCount.Should().Be(2);
        result.Value.Errors.Should().HaveCount(2);
        user.Usage.MonthlyLinksCreated.Should().Be(500);
    }

    [Fact]
    public async Task AllItemsInvalid_CreateNothingAndDoNotSave()
    {
        var user = GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 0);

        var result = await Sut().Handle(Command(valid: 0, invalid: 3), default);

        result.Value.SuccessCount.Should().Be(0);
        result.Value.FailureCount.Should().Be(3);
        user.Usage.MonthlyLinksCreated.Should().Be(0);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnlimitedPlan_IsNotQuotaLimited()
    {
        GivenUser(maxLinks: -1, bulk: true, usedThisMonth: 100000);

        (await Sut().Handle(Command(valid: 50), default)).Value.SuccessCount.Should().Be(50);
    }

    [Fact]
    public async Task ConcurrentUsageUpdate_ReturnsConcurrentUpdateError_LikeSingleCreation()
    {
        GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 0);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new ConcurrencyException("conflict", new Exception()));

        var result = await Sut().Handle(Command(valid: 2), default);

        result.Error.Should().Be(ShortUrlErrors.ConcurrentUpdate);
    }
}
