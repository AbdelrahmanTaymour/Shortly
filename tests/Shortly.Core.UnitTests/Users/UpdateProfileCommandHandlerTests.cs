using Shortly.Core.UnitTests.TestData;
using Shortly.Core.Users.UpdateProfile;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Users;

public class UpdateProfileCommandHandlerTests
{
    private readonly User _user = UserData.Create();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateProfileCommandHandler _sut;

    public UpdateProfileCommandHandlerTests()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _sut = new UpdateProfileCommandHandler(CallerData.Create(_user.Id), _users, _unitOfWork, TestClock.Provider());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheCallerNoLongerExists()
    {
        // Arrange
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(new UpdateProfileCommand("N", null, null, null, null, null, null, null), default);

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_StoreEveryField_AndSave()
    {
        // Act
        var result = await _sut.Handle(new UpdateProfileCommand("Jane", "Bio", "123", "https://jane.dev", "Acme", "Cairo", "EG", "UTC"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _user.Profile.Name.Should().Be("Jane");
        _user.Profile.Bio.Should().Be("Bio");
        _user.Profile.PhoneNumber.Should().Be("123");
        _user.Profile.Website.Should().Be("https://jane.dev");
        _user.Profile.Company.Should().Be("Acme");
        _user.Profile.Location.Should().Be("Cairo");
        _user.Profile.Country.Should().Be("EG");
        _user.Profile.TimeZone.Should().Be("UTC");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_KeepEveryOmittedField()
    {
        // Arrange
        _user.Profile.Update("Jane", "Bio", "123", "https://jane.dev", "Acme", "Cairo", "EG", "UTC", TestClock.UtcNow);

        // Act: a client that sends only the name must not wipe the rest
        await _sut.Handle(new UpdateProfileCommand("Jane D", null, null, null, null, null, null, null), default);

        // Assert
        _user.Profile.Name.Should().Be("Jane D");
        _user.Profile.Bio.Should().Be("Bio");
        _user.Profile.PhoneNumber.Should().Be("123");
        _user.Profile.Website.Should().Be("https://jane.dev");
        _user.Profile.Company.Should().Be("Acme");
        _user.Profile.Location.Should().Be("Cairo");
        _user.Profile.Country.Should().Be("EG");
        _user.Profile.TimeZone.Should().Be("UTC");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_Should_ClearAField_When_ItIsSentBlank(string blank)
    {
        // Arrange
        _user.Profile.Update("Jane", "Bio", "123", "https://jane.dev", "Acme", "Cairo", "EG", "UTC", TestClock.UtcNow);

        // Act
        await _sut.Handle(new UpdateProfileCommand(null, blank, null, blank, null, null, null, null), default);

        // Assert
        _user.Profile.Bio.Should().BeNull();
        _user.Profile.Website.Should().BeNull();
        _user.Profile.Name.Should().Be("Jane");
    }

    [Fact]
    public async Task Handle_Should_TrimTheValuesItStores()
    {
        // Act
        await _sut.Handle(new UpdateProfileCommand("  Jane  ", null, null, null, null, null, null, null), default);

        // Assert
        _user.Profile.Name.Should().Be("Jane");
    }
}
