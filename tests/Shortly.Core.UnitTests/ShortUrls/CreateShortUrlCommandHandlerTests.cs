using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Exceptions;
using Shortly.Core.ShortUrls.CreateShortUrl;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.ShortUrls;

public class CreateShortUrlCommandHandlerTests
{
    private const string UniqueIndex = "ux_short_urls_short_code";

    private static readonly CreateShortUrlCommand Plain =
        new("https://example.com/page", null, null, null, null, null, null, null);

    private readonly User _user = UserData.Create();
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly IShortCodeGenerator _generator = Substitute.For<IShortCodeGenerator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OwnerResolverData _resolver = new();
    private readonly IUserContext _caller;

    public CreateShortUrlCommandHandlerTests()
    {
        _caller = CallerData.Create(_user.Id);
        _caller.OwnerType.Returns(enShortUrlOwnerType.User);
        _caller.IpAddress.Returns("203.0.113.9");
        _resolver.WithUserOwner(_user).WithLimits(PlanLimitsData.With(maxLinks: 5, customCode: true));
        _generator.Generate(Arg.Any<int>()).Returns(Code("gen0001"));
    }

    private static ShortCode Code(string value) => ShortCode.Create(value).Value;

    private CreateShortUrlCommandHandler CreateSut(IUserContext? caller = null)
    {
        caller ??= _caller;
        return new CreateShortUrlCommandHandler(
            _shortUrls, _resolver.Create(caller), caller, _generator, _unitOfWork, TestClock.Provider());
    }

    private ShortUrl AddedLink()
    {
        var added = _shortUrls.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IShortUrlRepository.Add))
            .Select(call => (ShortUrl)call.GetArguments()[0]!)
            .Single();
        return added;
    }

    // ---- input and owner rules ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnTheUrlError_AndCreateNothing_When_TheUrlIsInvalid()
    {
        // Act
        var result = await CreateSut().Handle(Plain with { OriginalUrl = "not a url" }, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.OriginalUrlInvalid);
        _shortUrls.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnMonthlyLimitReached_AndCreateNothing_When_TheQuotaIsUsedUp()
    {
        // Arrange
        for (var i = 0; i < 5; i++) _user.Usage.IncrementLinksCreated(TestClock.UtcNow);

        // Act
        var result = await CreateSut().Handle(Plain, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.MonthlyLinkLimitReached);
        _shortUrls.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_AndPersistNothing_When_TheOrganizationClaimIsStale()
    {
        // Arrange
        var caller = CallerData.Create(_user.Id, organizationId: Guid.NewGuid());
        caller.OwnerType.Returns(enShortUrlOwnerType.Organization);
        _resolver.Authorization.ResolveClaimedMembershipAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ClaimedMembership>(OrganizationErrors.NotAMember));

        // Act
        var result = await CreateSut(caller).Handle(Plain, default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        _shortUrls.DidNotReceiveWithAnyArgs().Add(default!);
    }

    // ---- who owns the new link ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_CreateAPersonalLinkWithDefaults_AndCountItAgainstTheUsersQuota()
    {
        // Act
        var result = await CreateSut().Handle(Plain, default);

        // Assert
        result.Value.Should().Be("gen0001");
        var link = AddedLink();
        link.OwnerType.Should().Be(enShortUrlOwnerType.User);
        link.UserId.Should().Be(_user.Id);
        link.ClickLimit.Should().Be(-1);
        link.TrackingEnabled.Should().BeTrue();
        link.IsPrivate.Should().BeFalse();
        link.OriginalUrl.Value.Should().Be("https://example.com/page");
        _user.Usage.MonthlyLinksCreated.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_StoreTheOptionalSettings()
    {
        // Arrange
        var expiresAt = TestClock.UtcNow.AddDays(3);
        var command = new CreateShortUrlCommand("https://example.com", null, 25, false, true, expiresAt, "Title", "Description");

        // Act
        await CreateSut().Handle(command, default);

        // Assert
        var link = AddedLink();
        link.ClickLimit.Should().Be(25);
        link.TrackingEnabled.Should().BeFalse();
        link.IsPrivate.Should().BeTrue();
        link.ExpiresAt.Should().Be(expiresAt);
        link.Title.Should().Be("Title");
        link.Description.Should().Be("Description");
    }

    [Fact]
    public async Task Handle_Should_CreateAnOrganizationLink_RecordedAgainstTheOrganizationsQuota()
    {
        // Arrange
        var organization = OrganizationData.Create(plan: enSubscriptionPlan.Professional);
        var member = MemberData.Create(organization.Id, _user.Id, MemberRole.Member);
        var caller = CallerData.Create(_user.Id, organizationId: organization.Id);
        caller.OwnerType.Returns(enShortUrlOwnerType.Organization);
        _resolver.Authorization.ResolveClaimedMembershipAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ClaimedMembership(member)));
        _resolver.Organizations.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        // Act
        var result = await CreateSut(caller).Handle(Plain, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var link = AddedLink();
        link.OwnerType.Should().Be(enShortUrlOwnerType.Organization);
        link.OrganizationId.Should().Be(organization.Id);
        link.CreatedByMemberId.Should().Be(member.Id);
        link.UserId.Should().BeNull();
        organization.Usage.MonthlyLinksCreated.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_CreateAnAnonymousLink_WithTheSessionAndIpAddress()
    {
        // Arrange
        var caller = Substitute.For<IUserContext>();
        caller.OwnerType.Returns(enShortUrlOwnerType.Anonymous);
        caller.AnonymousSessionId.Returns("session-1");
        caller.IpAddress.Returns("203.0.113.9");

        // Act
        var result = await CreateSut(caller).Handle(Plain, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var link = AddedLink();
        link.OwnerType.Should().Be(enShortUrlOwnerType.Anonymous);
        link.AnonymousSessionId.Should().Be("session-1");
        link.IpAddress.Should().Be("203.0.113.9");
        link.TrackingEnabled.Should().BeFalse();
    }

    // ---- the short code ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnCustomShortCodeNotAllowed_When_ThePlanLacksTheFeature()
    {
        // Arrange
        _resolver.WithLimits(PlanLimitsData.With(customCode: false));

        // Act
        var result = await CreateSut().Handle(Plain with { CustomShortCode = "mycode1" }, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.CustomShortCodeNotAllowed);
        _shortUrls.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_UseTheCustomCode_When_ItIsValidFreeAndAllowed()
    {
        // Act
        var result = await CreateSut().Handle(Plain with { CustomShortCode = "mycode1" }, default);

        // Assert
        result.Value.Should().Be("mycode1");
        AddedLink().ShortCode.Value.Should().Be("mycode1");
        _generator.DidNotReceiveWithAnyArgs().Generate();
    }

    [Fact]
    public async Task Handle_Should_ReturnTheCodeError_When_TheCustomCodeIsInvalid()
    {
        // Act
        var result = await CreateSut().Handle(Plain with { CustomShortCode = "ab" }, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeInvalidLength);
    }

    [Fact]
    public async Task Handle_Should_ReturnShortCodeAlreadyExists_When_TheCustomCodeIsTaken()
    {
        // Arrange
        _shortUrls.ExistsByShortCodeAsync(Arg.Is<ShortCode>(c => c.Value == "mycode1"), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await CreateSut().Handle(Plain with { CustomShortCode = "mycode1" }, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeAlreadyExists);
        _shortUrls.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_GenerateAnotherCode_When_TheFirstGeneratedOneExists()
    {
        // Arrange
        _generator.Generate(Arg.Any<int>()).Returns(Code("taken01"), Code("free001"));
        _shortUrls.ExistsByShortCodeAsync(Arg.Is<ShortCode>(c => c.Value == "taken01"), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await CreateSut().Handle(Plain, default);

        // Assert
        result.Value.Should().Be("free001");
    }

    [Fact]
    public async Task Handle_Should_ReturnShortCodeGenerationFailed_AfterFiveGeneratedCodesAreAllTaken()
    {
        // Arrange
        _shortUrls.ExistsByShortCodeAsync(Arg.Any<ShortCode>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await CreateSut().Handle(Plain, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeGenerationFailed);
        _generator.Received(5).Generate(Arg.Any<int>());
        _shortUrls.DidNotReceiveWithAnyArgs().Add(default!);
    }

    // ---- domain rules ------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_AndCreateNothing_When_TheExpirationIsInThePast()
    {
        // Act
        var result = await CreateSut().Handle(Plain with { ExpiresAt = TestClock.UtcNow.AddMinutes(-1) }, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidExpiration);
        _shortUrls.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_When_TheClickLimitIsNotPositive()
    {
        // Act
        var result = await CreateSut().Handle(Plain with { ClickLimit = 0 }, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidClickLimit);
    }

    // ---- races on save -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnConcurrentUpdate_When_TheUsageCounterWasChangedByAnotherRequest()
    {
        // Arrange
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new ConcurrencyException("conflict", new Exception()));

        // Act
        var result = await CreateSut().Handle(Plain, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ConcurrentUpdate);
    }

    [Fact]
    public async Task Handle_Should_ReturnShortCodeAlreadyExists_When_AnotherRequestTookTheChosenCodeFirst()
    {
        // Arrange
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new UniqueConstraintViolationException(UniqueIndex));

        // Act
        var result = await CreateSut().Handle(Plain with { CustomShortCode = "mycode1" }, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeAlreadyExists);
    }

    [Fact]
    public async Task Handle_Should_ReplaceAGeneratedCode_AndSaveAgain_When_AnotherRequestTookItFirst_WithoutCountingTheLinkTwice()
    {
        // Arrange
        _generator.Generate(Arg.Any<int>()).Returns(Code("first001"), Code("second01"));
        var attempts = 0;
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
            ++attempts == 1 ? throw new UniqueConstraintViolationException(UniqueIndex) : Task.FromResult(1));

        // Act
        var result = await CreateSut().Handle(Plain, default);

        // Assert
        result.Value.Should().Be("second01");
        attempts.Should().Be(2);
        _user.Usage.MonthlyLinksCreated.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_ReturnShortCodeGenerationFailed_When_EveryRetryCollides()
    {
        // Arrange
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new UniqueConstraintViolationException(UniqueIndex));

        // Act
        var result = await CreateSut().Handle(Plain, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeGenerationFailed);
        await _unitOfWork.Received(5).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotHideAnUnrelatedConstraintViolation()
    {
        // Arrange
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new UniqueConstraintViolationException("some_other_index"));

        // Act
        var act = () => CreateSut().Handle(Plain, default);

        // Assert
        await act.Should().ThrowAsync<UniqueConstraintViolationException>();
    }
}
