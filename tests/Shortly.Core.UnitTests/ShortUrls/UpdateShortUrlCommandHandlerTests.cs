using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Exceptions;
using Shortly.Core.ShortUrls.UpdateShortUrl;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.ShortUrls;

public class UpdateShortUrlCommandHandlerTests
{
    private const string CurrentCode = "abc1234";

    private readonly User _owner = UserData.Create();
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OwnerResolverData _resolver = new();

    public UpdateShortUrlCommandHandlerTests() =>
        _resolver.WithUserOwner(_owner).WithLimits(PlanLimitsData.With(customCode: true));

    private ShortUrl OwnLink() => ShortUrlData.UserLink(_owner.Id, CurrentCode);

    private UpdateShortUrlCommand Command(
        string? shortCode = null, string originalUrl = "https://example.org/new", int clickLimit = 10,
        bool isActive = true, DateTime? expiresAt = null) =>
        new(1, originalUrl, shortCode ?? CurrentCode, "New title", "New description", clickLimit, isActive,
            IsPrivate: true, TrackingEnabled: false, expiresAt);

    private Task<Result> Handle(ShortUrl? link, UpdateShortUrlCommand command, IUserContext? caller = null)
    {
        _shortUrls.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(link);
        caller ??= CallerData.Create(_owner.Id);
        var sut = new UpdateShortUrlCommandHandler(
            _shortUrls, caller, Substitute.For<IOrganizationAuthorizationService>(), _resolver.Create(caller),
            _unitOfWork, TestClock.Provider());
        return sut.Handle(command, default);
    }

    // ---- access ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheLinkDoesNotExist()
    {
        // Act
        var result = await Handle(null, Command());

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_AndChangeNothing_When_TheLinkBelongsToSomeoneElse()
    {
        // Arrange
        var link = ShortUrlData.UserLink(Guid.NewGuid(), CurrentCode);

        // Act
        var result = await Handle(link, Command());

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
        link.Title.Should().BeNull();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---- input -------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnTheUrlError_When_TheNewUrlIsInvalid()
    {
        // Act
        var result = await Handle(OwnLink(), Command(originalUrl: "not a url"));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.OriginalUrlInvalid);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheCodeError_When_TheNewCodeIsInvalid()
    {
        // Act
        var result = await Handle(OwnLink(), Command(shortCode: "ab"));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeInvalidLength);
    }

    // ---- editing without changing the code ---------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_UpdateTheEditableProperties_AndSave()
    {
        // Arrange
        var link = OwnLink();
        var expiresAt = TestClock.UtcNow.AddDays(7);

        // Act
        var result = await Handle(link, Command(expiresAt: expiresAt, isActive: false));

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.OriginalUrl.Value.Should().Be("https://example.org/new");
        link.Title.Should().Be("New title");
        link.Description.Should().Be("New description");
        link.ClickLimit.Should().Be(10);
        link.IsActive.Should().BeFalse();
        link.IsPrivate.Should().BeTrue();
        link.TrackingEnabled.Should().BeFalse();
        link.ExpiresAt.Should().Be(expiresAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ClearTheExpiration_When_NullIsSent()
    {
        // Arrange
        var link = ShortUrl.CreateForUser(_owner.Id, ShortUrlData.UserLink(_owner.Id).OriginalUrl,
            ShortCode.Create(CurrentCode).Value, TestClock.UtcNow, expiresAt: TestClock.UtcNow.AddDays(1)).Value;

        // Act
        await Handle(link, Command(expiresAt: null));

        // Assert
        link.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_KeepThePasswordProtection_WhenEditingOtherProperties()
    {
        // Arrange
        var link = OwnLink();
        link.ProtectWithPassword("hash", TestClock.UtcNow);

        // Act
        await Handle(link, Command());

        // Assert
        link.IsPasswordProtected.Should().BeTrue();
        link.PasswordHash.Should().Be("hash");
    }

    [Fact]
    public async Task Handle_Should_NotRequireTheCustomCodeFeature_NorCheckTheCode_When_TheCodeIsUnchanged()
    {
        // Arrange: a plan without custom codes can still edit its own links
        _resolver.WithLimits(PlanLimitsData.With(customCode: false));

        // Act
        var result = await Handle(OwnLink(), Command());

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _shortUrls.DidNotReceiveWithAnyArgs().ExistsByShortCodeAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_AndSaveNothing_When_TheClickLimitIsInvalid()
    {
        // Act
        var result = await Handle(OwnLink(), Command(clickLimit: 0));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidClickLimit);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_When_TheExpirationIsInThePast()
    {
        // Act
        var result = await Handle(OwnLink(), Command(expiresAt: TestClock.UtcNow.AddMinutes(-1)));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidExpiration);
    }

    // ---- changing the code -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnCustomShortCodeNotAllowed_When_TheCodeChangesAndThePlanLacksTheFeature()
    {
        // Arrange
        _resolver.WithLimits(PlanLimitsData.With(customCode: false));
        var link = OwnLink();

        // Act
        var result = await Handle(link, Command(shortCode: "newcode1"));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.CustomShortCodeNotAllowed);
        link.ShortCode.Value.Should().Be(CurrentCode);
    }

    [Fact]
    public async Task Handle_Should_ReturnShortCodeAlreadyExists_When_TheNewCodeIsTaken()
    {
        // Arrange
        _shortUrls.ExistsByShortCodeAsync(Arg.Is<ShortCode>(c => c.Value == "newcode1"), Arg.Any<CancellationToken>()).Returns(true);
        var link = OwnLink();

        // Act
        var result = await Handle(link, Command(shortCode: "newcode1"));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeAlreadyExists);
        link.ShortCode.Value.Should().Be(CurrentCode);
    }

    [Fact]
    public async Task Handle_Should_ChangeTheCode_When_ItIsFreeAndThePlanAllowsCustomCodes()
    {
        // Arrange
        var link = OwnLink();

        // Act
        var result = await Handle(link, Command(shortCode: "newcode1"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.ShortCode.Value.Should().Be("newcode1");
    }

    [Fact]
    public async Task Handle_Should_TreatCodesAsCaseSensitive_SoChangingOnlyTheCaseIsACodeChange()
    {
        // Arrange
        _resolver.WithLimits(PlanLimitsData.With(customCode: false));

        // Act
        var result = await Handle(OwnLink(), Command(shortCode: CurrentCode.ToUpperInvariant()));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.CustomShortCodeNotAllowed);
    }

    // ---- concurrency -------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnShortCodeAlreadyExists_When_AnotherRequestTookTheCodeFirst()
    {
        // Arrange
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new UniqueConstraintViolationException("ux_short_urls_short_code"));

        // Act
        var result = await Handle(OwnLink(), Command(shortCode: "newcode1"));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeAlreadyExists);
    }

    [Fact]
    public async Task Handle_Should_NotHideAnUnrelatedConstraintViolation()
    {
        // Arrange
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new UniqueConstraintViolationException("some_other_index"));

        // Act
        var act = () => Handle(OwnLink(), Command());

        // Assert
        await act.Should().ThrowAsync<UniqueConstraintViolationException>();
    }
}
