using FluentValidation.TestHelper;
using Shortly.Core.Organizations.TransferOwnership;

namespace Shortly.Core.UnitTests.Organizations;

public class TransferOwnershipCommandValidatorTests
{
    private readonly TransferOwnershipCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new TransferOwnershipCommand(Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new TransferOwnershipCommand(Guid.Empty, Guid.NewGuid()));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InNewOwnerId()
    {
        // Act
        var result = _sut.TestValidate(new TransferOwnershipCommand(Guid.NewGuid(), Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewOwnerId);
    }
}
