using Shortly.Domain.UnitTests.TestData;

namespace Shortly.Domain.UnitTests.Users;

public class UserProfileTests
{
    private static readonly DateTime Later = TestClock.Now.AddHours(1);

    [Fact]
    public void NewProfile_Should_StartEmpty()
    {
        // Act
        var profile = UserData.CreateUser().Profile;

        // Assert
        profile.Name.Should().BeNull();
        profile.Bio.Should().BeNull();
        profile.PhoneNumber.Should().BeNull();
        profile.ProfilePictureUrl.Should().BeNull();
        profile.Website.Should().BeNull();
        profile.Company.Should().BeNull();
        profile.Location.Should().BeNull();
        profile.Country.Should().BeNull();
        profile.TimeZone.Should().BeNull();
    }

    [Fact]
    public void Update_Should_SetEveryEditableField_AndTouchTheTimestamp()
    {
        // Arrange
        var profile = UserData.CreateUser().Profile;

        // Act
        var result = profile.Update("Jane Doe", "Bio", "+1 555 0100", "https://jane.dev", "Acme", "Berlin",
            "Germany", "Europe/Berlin", Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Name.Should().Be("Jane Doe");
        profile.Bio.Should().Be("Bio");
        profile.PhoneNumber.Should().Be("+1 555 0100");
        profile.Website.Should().Be("https://jane.dev");
        profile.Company.Should().Be("Acme");
        profile.Location.Should().Be("Berlin");
        profile.Country.Should().Be("Germany");
        profile.TimeZone.Should().Be("Europe/Berlin");
        profile.UpdatedAtUtc.Should().Be(Later);
    }

    [Fact]
    public void Update_Should_ClearFields_When_NullIsPassed()
    {
        // Arrange
        var profile = UserData.CreateUser().Profile;
        profile.Update("Jane", "Bio", "1", "site", "Acme", "Berlin", "DE", "UTC", TestClock.Now);

        // Act
        profile.Update(null, null, null, null, null, null, null, null, Later);

        // Assert
        profile.Name.Should().BeNull();
        profile.Bio.Should().BeNull();
        profile.Company.Should().BeNull();
    }

    [Fact]
    public void Update_Should_NotTouchTheProfilePicture()
    {
        // Arrange
        var profile = UserData.CreateUser().Profile;
        profile.UpdateProfilePicture("https://example.com/pic.png", TestClock.Now);

        // Act
        profile.Update("Jane", null, null, null, null, null, null, null, Later);

        // Assert
        profile.ProfilePictureUrl.Should().Be("https://example.com/pic.png");
    }

    [Fact]
    public void UpdateName_Should_SetOnlyTheName()
    {
        // Arrange
        var profile = UserData.CreateUser().Profile;
        profile.Update("Old", "Bio", null, null, null, null, null, null, TestClock.Now);

        // Act
        var result = profile.UpdateName("New", Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Name.Should().Be("New");
        profile.Bio.Should().Be("Bio");
        profile.UpdatedAtUtc.Should().Be(Later);
    }

    [Fact]
    public void UpdateProfilePicture_Should_SetAndClearTheUrl()
    {
        // Arrange
        var profile = UserData.CreateUser().Profile;

        // Act
        profile.UpdateProfilePicture("https://example.com/pic.png", TestClock.Now);
        var afterSet = profile.ProfilePictureUrl;
        var result = profile.UpdateProfilePicture(null, Later);

        // Assert
        afterSet.Should().Be("https://example.com/pic.png");
        result.IsSuccess.Should().BeTrue();
        profile.ProfilePictureUrl.Should().BeNull();
        profile.UpdatedAtUtc.Should().Be(Later);
    }
}
