using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.ShortUrls.SetShortUrlPassword;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.ShortUrls;

public class SetShortUrlPasswordCommandHandlerTests
{
    private static readonly SetShortUrlPasswordCommand Command = new(1, "Str0ng-password!");

    private readonly User _owner = UserData.Create();
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OwnerResolverData _resolver = new();

    public SetShortUrlPasswordCommandHandlerTests()
    {
        _passwordHasher.Hash(Command.Password).Returns("hashed-link-password");
        _resolver.WithUserOwner(_owner).WithLimits(PlanLimitsData.With(protection: true));
    }

    private Task<Result> Handle(ShortUrl? link, IUserContext caller)
    {
        _shortUrls.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(link);
        var sut = new SetShortUrlPasswordCommandHandler(
            _shortUrls, caller, Substitute.For<IOrganizationAuthorizationService>(), _resolver.Create(caller),
            _passwordHasher, _unitOfWork, TestClock.Provider());
        return sut.Handle(Command, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheLinkDoesNotExist()
    {
        // Act
        var result = await Handle(null, CallerData.Create(_owner.Id));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_AndProtectNothing_When_TheLinkBelongsToSomeoneElse()
    {
        // Arrange
        var link = ShortUrlData.UserLink(Guid.NewGuid());

        // Act
        var result = await Handle(link, CallerData.Create(_owner.Id));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
        link.IsPasswordProtected.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnLinkProtectionNotAllowed_When_TheOwnersPlanLacksIt()
    {
        // Arrange
        _resolver.WithLimits(PlanLimitsData.With(protection: false));
        var link = ShortUrlData.UserLink(_owner.Id);

        // Act
        var result = await Handle(link, CallerData.Create(_owner.Id));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.LinkProtectionNotAllowed);
        link.IsPasswordProtected.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_StoreOnlyTheHashOfThePassword_AndSave_When_ThePlanAllowsProtection()
    {
        // Arrange
        var link = ShortUrlData.UserLink(_owner.Id);

        // Act
        var result = await Handle(link, CallerData.Create(_owner.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.IsPasswordProtected.Should().BeTrue();
        link.PasswordHash.Should().Be("hashed-link-password");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReplaceTheHash_When_TheLinkIsAlreadyProtected()
    {
        // Arrange
        var link = ShortUrlData.UserLink(_owner.Id);
        link.ProtectWithPassword("old-hash", TestClock.UtcNow);

        // Act
        await Handle(link, CallerData.Create(_owner.Id));

        // Assert
        link.PasswordHash.Should().Be("hashed-link-password");
    }

    [Fact]
    public async Task Handle_Should_UseTheLinkOwnersPlan_WhenASystemAdminProtectsSomeoneElsesLink()
    {
        // Arrange: the admin has no plan of their own; the link owner's plan decides
        var link = ShortUrlData.UserLink(_owner.Id);

        // Act
        var result = await Handle(link, CallerData.Create(Guid.NewGuid(), isAdmin: true));

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _resolver.Users.Received(1).GetByIdAsync(_owner.Id, Arg.Any<CancellationToken>());
    }
}
