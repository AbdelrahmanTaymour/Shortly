using FluentValidation.TestHelper;
using Shortly.Core.Users.UpdateProfile;

namespace Shortly.Core.UnitTests.Users;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _sut = new();

    private static UpdateProfileCommand Empty() => new(null, null, null, null, null, null, null, null);

    [Fact]
    public void Validate_Should_Pass_WhenEveryFieldIsOmitted()
    {
        // Act
        var result = _sut.TestValidate(Empty());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(nameof(UpdateProfileCommand.Name), 100)]
    [InlineData(nameof(UpdateProfileCommand.Bio), 500)]
    [InlineData(nameof(UpdateProfileCommand.PhoneNumber), 20)]
    [InlineData(nameof(UpdateProfileCommand.Website), 500)]
    [InlineData(nameof(UpdateProfileCommand.Company), 100)]
    [InlineData(nameof(UpdateProfileCommand.Location), 100)]
    [InlineData(nameof(UpdateProfileCommand.Country), 50)]
    [InlineData(nameof(UpdateProfileCommand.TimeZone), 50)]
    public void Validate_Should_EnforceEachFieldLengthBoundary(string property, int max)
    {
        // Arrange
        UpdateProfileCommand With(int length) => property switch
        {
            nameof(UpdateProfileCommand.Name) => Empty() with { Name = new string('a', length) },
            nameof(UpdateProfileCommand.Bio) => Empty() with { Bio = new string('a', length) },
            nameof(UpdateProfileCommand.PhoneNumber) => Empty() with { PhoneNumber = new string('a', length) },
            nameof(UpdateProfileCommand.Website) => Empty() with { Website = new string('a', length) },
            nameof(UpdateProfileCommand.Company) => Empty() with { Company = new string('a', length) },
            nameof(UpdateProfileCommand.Location) => Empty() with { Location = new string('a', length) },
            nameof(UpdateProfileCommand.Country) => Empty() with { Country = new string('a', length) },
            _ => Empty() with { TimeZone = new string('a', length) }
        };

        // Act
        var atLimit = _sut.TestValidate(With(max));
        var overLimit = _sut.TestValidate(With(max + 1));

        // Assert
        atLimit.ShouldNotHaveAnyValidationErrors();
        overLimit.ShouldHaveValidationErrorFor(property);
    }
}
