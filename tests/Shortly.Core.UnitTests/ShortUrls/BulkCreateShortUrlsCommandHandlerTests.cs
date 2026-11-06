using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Exceptions;
using Shortly.Core.ShortUrls.BulkCreateShortUrls;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.ShortUrls;

public class BulkCreateShortUrlsCommandHandlerTests
{
    private const string UniqueIndex = "ux_short_urls_short_code";

    private static readonly DateTime Now = TestClock.UtcNow;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly IShortUrlRepository _links = Substitute.For<IShortUrlRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OwnerResolverData _resolver = new();
    private readonly IUserContext _caller;
    private IShortCodeGenerator _generator = new ShortCodeGenerator();

    public BulkCreateShortUrlsCommandHandlerTests()
    {
        _links.GetExistingShortCodesAsync(Arg.Any<IReadOnlyCollection<ShortCode>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());
        _caller = CallerData.Create(_userId);
        _caller.OwnerType.Returns(enShortUrlOwnerType.User);
        _caller.IpAddress.Returns("203.0.113.1");
    }

    private User GivenUser(int maxLinks, bool bulk, int usedThisMonth)
    {
        var user = UserData.Create(email: $"{Guid.NewGuid():N}@example.com");
        for (var i = 0; i < usedThisMonth; i++) user.Usage.IncrementLinksCreated(Now);
        _resolver.Users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _resolver.WithLimits(PlanLimitsData.With(maxLinks, bulk));
        return user;
    }

    private BulkCreateShortUrlsCommandHandler CreateSut(IUserContext? caller = null)
    {
        caller ??= _caller;
        return new BulkCreateShortUrlsCommandHandler(
            _links, caller, _resolver.Create(caller), _unitOfWork, _generator, TestClock.Provider());
    }

    private static BulkCreateShortUrlsCommand Command(int valid, int invalid = 0) => new(
        Enumerable.Range(0, valid).Select(i => new BulkCreateShortUrlItem($"https://example.com/{i}", null, null))
            .Concat(Enumerable.Range(0, invalid).Select(_ => new BulkCreateShortUrlItem("not a url", null, null)))
            .ToList());

    private IReadOnlyList<ShortUrl> AddedLinks() =>
        _links.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IShortUrlRepository.Add))
            .Select(call => (ShortUrl)call.GetArguments()[0]!)
            .ToList();

    private static ShortCode Code(string value) => ShortCode.Create(value).Value;

    // ---- entitlement and quota ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnBulkCreationNotAllowed_AndCreateNothing_When_ThePlanLacksBulkCreation()
    {
        // Arrange
        GivenUser(maxLinks: 100, bulk: false, usedThisMonth: 0);

        // Act
        var result = await CreateSut().Handle(Command(valid: 3), default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.BulkCreationNotAllowed);
        _links.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateEveryValidItem_AndIncreaseTheUsageByTheirCount()
    {
        // Arrange
        var user = GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 10);

        // Act
        var result = await CreateSut().Handle(Command(valid: 4), default);

        // Assert
        result.Value.SuccessCount.Should().Be(4);
        result.Value.FailureCount.Should().Be(0);
        AddedLinks().Should().HaveCount(4);
        user.Usage.MonthlyLinksCreated.Should().Be(14);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RejectTheWholeRequest_When_TheValidItemsExceedTheRemainingQuota()
    {
        // Arrange
        var user = GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 498);

        // Act
        var result = await CreateSut().Handle(Command(valid: 3), default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.MonthlyLinkLimitReached);
        _links.DidNotReceiveWithAnyArgs().Add(default!);
        user.Usage.MonthlyLinksCreated.Should().Be(498);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Accept_When_TheValidItemsExactlyFillTheQuota()
    {
        // Arrange
        var user = GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 497);

        // Act
        var result = await CreateSut().Handle(Command(valid: 3), default);

        // Assert
        result.Value.SuccessCount.Should().Be(3);
        user.Usage.MonthlyLinksCreated.Should().Be(500);
    }

    [Fact]
    public async Task Handle_Should_OnlyCountValidItemsAgainstTheQuota_AndReportTheInvalidOnesAsFailures()
    {
        // Arrange
        var user = GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 497);

        // Act: 3 valid items fit the remaining 3, even though the request has 5 items
        var result = await CreateSut().Handle(Command(valid: 3, invalid: 2), default);

        // Assert
        result.Value.TotalProcessed.Should().Be(5);
        result.Value.SuccessCount.Should().Be(3);
        result.Value.FailureCount.Should().Be(2);
        result.Value.Errors.Should().HaveCount(2).And.OnlyContain(e => e.StartsWith("not a url:"));
        user.Usage.MonthlyLinksCreated.Should().Be(500);
    }

    [Fact]
    public async Task Handle_Should_CreateNothingAndNotSave_When_EveryItemIsInvalid()
    {
        // Arrange
        var user = GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 0);

        // Act
        var result = await CreateSut().Handle(Command(valid: 0, invalid: 3), default);

        // Assert
        result.Value.SuccessCount.Should().Be(0);
        result.Value.FailureCount.Should().Be(3);
        user.Usage.MonthlyLinksCreated.Should().Be(0);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotLimitTheQuota_OnAnUnlimitedPlan()
    {
        // Arrange
        GivenUser(maxLinks: -1, bulk: true, usedThisMonth: 100_000);

        // Act
        var result = await CreateSut().Handle(Command(valid: 50), default);

        // Assert
        result.Value.SuccessCount.Should().Be(50);
    }

    // ---- who owns the links ------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_CreateOrganizationLinks_ForTheDatabaseMember_AndCountThemAgainstTheOrganization()
    {
        // Arrange
        var organization = OrganizationData.Create(plan: enSubscriptionPlan.Professional);
        var member = MemberData.Create(organization.Id, _userId, MemberRole.Member);
        var caller = CallerData.Create(_userId, organizationId: organization.Id);
        caller.OwnerType.Returns(enShortUrlOwnerType.Organization);
        _resolver.Authorization.ResolveClaimedMembershipAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ClaimedMembership(member)));
        _resolver.Organizations.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);
        _resolver.WithLimits(PlanLimitsData.With(maxLinks: 500, bulk: true));

        // Act
        var result = await CreateSut(caller).Handle(Command(valid: 2), default);

        // Assert
        result.Value.SuccessCount.Should().Be(2);
        AddedLinks().Should().OnlyContain(l =>
            l.OrganizationId == organization.Id && l.CreatedByMemberId == member.Id && l.UserId == null);
        organization.Usage.MonthlyLinksCreated.Should().Be(2);
    }

    // ---- short codes -------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_GiveEveryLinkADifferentCode_EvenWhenTheGeneratorRepeatsItself()
    {
        // Arrange
        GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 0);
        var generator = Substitute.For<IShortCodeGenerator>();
        generator.Generate(Arg.Any<int>()).Returns(Code("dup0001"), Code("dup0001"), Code("uniq002"));
        _generator = generator;

        // Act
        var result = await CreateSut().Handle(Command(valid: 2), default);

        // Assert
        result.Value.SuccessCount.Should().Be(2);
        AddedLinks().Select(l => l.ShortCode.Value).Should().BeEquivalentTo("dup0001", "uniq002");
    }

    [Fact]
    public async Task Handle_Should_ReplaceGeneratedCodesThatAlreadyExistInTheDatabase()
    {
        // Arrange
        GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 0);
        var generator = Substitute.For<IShortCodeGenerator>();
        generator.Generate(Arg.Any<int>()).Returns(Code("taken01"), Code("fresh01"));
        _generator = generator;
        _links.GetExistingShortCodesAsync(Arg.Any<IReadOnlyCollection<ShortCode>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "taken01" });

        // Act
        await CreateSut().Handle(Command(valid: 1), default);

        // Assert
        AddedLinks().Single().ShortCode.Value.Should().Be("fresh01");
    }

    [Fact]
    public async Task Handle_Should_GiveEveryLinkAFreshCode_AndSaveAgain_When_AnotherRequestTookACodeFirst()
    {
        // Arrange
        GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 0);
        var generator = Substitute.For<IShortCodeGenerator>();
        generator.Generate(Arg.Any<int>()).Returns(Code("first001"), Code("second01"));
        _generator = generator;
        var attempts = 0;
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
            ++attempts == 1 ? throw new UniqueConstraintViolationException(UniqueIndex) : Task.FromResult(1));

        // Act
        var result = await CreateSut().Handle(Command(valid: 1), default);

        // Assert
        result.Value.SuccessCount.Should().Be(1);
        attempts.Should().Be(2);
        AddedLinks().Single().ShortCode.Value.Should().Be("second01");
    }

    [Fact]
    public async Task Handle_Should_ReturnShortCodeGenerationFailed_When_EveryRetryCollides()
    {
        // Arrange
        GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 0);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new UniqueConstraintViolationException(UniqueIndex));

        // Act
        var result = await CreateSut().Handle(Command(valid: 2), default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeGenerationFailed);
        await _unitOfWork.Received(3).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotHideAnUnrelatedConstraintViolation()
    {
        // Arrange
        GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 0);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new UniqueConstraintViolationException("some_other_index"));

        // Act
        var act = () => CreateSut().Handle(Command(valid: 1), default);

        // Assert
        await act.Should().ThrowAsync<UniqueConstraintViolationException>();
    }

    // ---- concurrency -------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnConcurrentUpdate_When_TheUsageCounterWasChangedByAnotherRequest()
    {
        // Arrange
        GivenUser(maxLinks: 500, bulk: true, usedThisMonth: 0);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new ConcurrencyException("conflict", new Exception()));

        // Act
        var result = await CreateSut().Handle(Command(valid: 2), default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ConcurrentUpdate);
    }
}
