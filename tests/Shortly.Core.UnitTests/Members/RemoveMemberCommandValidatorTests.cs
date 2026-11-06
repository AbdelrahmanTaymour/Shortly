using FluentValidation.TestHelper;
using Shortly.Core.Members.RemoveMember;

namespace Shortly.Core.UnitTests.Members;

public class RemoveMemberCommandValidatorTests
{
    private readonly RemoveMemberCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new RemoveMemberCommand(Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new RemoveMemberCommand(Guid.Empty, Guid.NewGuid()));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InUserId()
    {
        // Act
        var result = _sut.TestValidate(new RemoveMemberCommand(Guid.NewGuid(), Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
