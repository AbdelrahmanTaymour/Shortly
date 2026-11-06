using FluentValidation.TestHelper;
using Shortly.Core.Admin.BulkDeactivateUsers;

namespace Shortly.Core.UnitTests.Admin;

public class BulkDeactivateUsersCommandValidatorTests
{
    private readonly BulkDeactivateUsersCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForOneOrMoreUserIds()
    {
        // Act
        var result = _sut.TestValidate(new BulkDeactivateUsersCommand([Guid.NewGuid(), Guid.NewGuid()]));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForAnEmptyList()
    {
        // Act
        var result = _sut.TestValidate(new BulkDeactivateUsersCommand([]));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserIds);
    }

    [Fact]
    public void Validate_Should_Fail_WhenAnyIdIsBlank()
    {
        // Act
        var result = _sut.TestValidate(new BulkDeactivateUsersCommand([Guid.NewGuid(), Guid.Empty]));

        // Assert
        result.ShouldHaveValidationErrorFor("UserIds[1]");
    }
}
