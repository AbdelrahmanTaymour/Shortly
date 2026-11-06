using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Exceptions;
using Shortly.Core.ShortUrls.AssignShortCode;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.ShortUrls;

public class AssignShortCodeCommandHandlerTests
{
    private readonly User _owner = UserData.Create();
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OwnerResolverData _resolver = new();

    public AssignShortCodeCommandHandlerTests() =>
        _resolver.WithUserOwner(_owner).WithLimits(PlanLimitsData.With(customCode: true));

    private Task<Result> Handle(ShortUrl? link, string newCode, IUserContext? caller = null)
    {
        _shortUrls.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(link);
        caller ??= CallerData.Create(_owner.Id);
        var sut = new AssignShortCodeCommandHandler(
            _shortUrls, caller, Substitute.For<IOrganizationAuthorizationService>(), _resolver.Create(caller),
            _unitOfWork, TestClock.Provider());
        return sut.Handle(new AssignShortCodeCommand(1, newCode), default);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheLinkDoesNotExist()
    {
        // Act
        var result = await Handle(null, "custom01");

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_AndChangeNothing_When_TheLinkBelongsToSomeoneElse()
    {
        // Arrange
        var link = ShortUrlData.UserLink(Guid.NewGuid());

        // Act
        var result = await Handle(link, "custom01");

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
        link.ShortCode.Value.Should().Be("abc1234");
    }

    [Fact]
    public async Task Handle_Should_ReturnCustomShortCodeNotAllowed_When_ThePlanLacksTheFeature()
    {
        // Arrange
        _resolver.WithLimits(PlanLimitsData.With(customCode: false));
        var link = ShortUrlData.UserLink(_owner.Id);

        // Act
        var result = await Handle(link, "custom01");

        // Assert
        result.Error.Should().Be(ShortUrlErrors.CustomShortCodeNotAllowed);
        link.ShortCode.Value.Should().Be("abc1234");
    }

    [Fact]
    public async Task Handle_Should_ReturnTheCodeError_When_TheNewCodeIsInvalid()
    {
        // Act
        var result = await Handle(ShortUrlData.UserLink(_owner.Id), "ab");

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeInvalidLength);
    }

    [Fact]
    public async Task Handle_Should_ReturnShortCodeAlreadyExists_When_TheCodeIsTaken()
    {
        // Arrange
        _shortUrls.ExistsByShortCodeAsync(Arg.Is<ShortCode>(c => c.Value == "custom01"), Arg.Any<CancellationToken>()).Returns(true);
        var link = ShortUrlData.UserLink(_owner.Id);

        // Act
        var result = await Handle(link, "custom01");

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeAlreadyExists);
        link.ShortCode.Value.Should().Be("abc1234");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_AssignTheCode_AndSave_When_ItIsFreeAndThePlanAllowsIt()
    {
        // Arrange
        var link = ShortUrlData.UserLink(_owner.Id);

        // Act
        var result = await Handle(link, "custom01");

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.ShortCode.Value.Should().Be("custom01");
        link.UpdatedAtUtc.Should().Be(TestClock.UtcNow);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnShortCodeAlreadyExists_When_AnotherRequestTookTheCodeFirst()
    {
        // Arrange
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new UniqueConstraintViolationException("ux_short_urls_short_code"));

        // Act
        var result = await Handle(ShortUrlData.UserLink(_owner.Id), "custom01");

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeAlreadyExists);
    }
}
