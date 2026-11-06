using FluentValidation.TestHelper;
using Shortly.Core.Admin.BulkDeleteUsers;

namespace Shortly.Core.UnitTests.Admin;

public class BulkDeleteUsersCommandValidatorTests
{
    private readonly BulkDeleteUsersCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForOneOrMoreUserIds()
    {
        // Act
        var result = _sut.TestValidate(new BulkDeleteUsersCommand([Guid.NewGuid(), Guid.NewGuid()]));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForAnEmptyList()
    {
        // Act
        var result = _sut.TestValidate(new BulkDeleteUsersCommand([]));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserIds);
    }

    [Fact]
    public void Validate_Should_Fail_WhenAnyIdIsBlank()
    {
        // Act
        var result = _sut.TestValidate(new BulkDeleteUsersCommand([Guid.NewGuid(), Guid.Empty]));

        // Assert
        result.ShouldHaveValidationErrorFor("UserIds[1]");
    }
}
