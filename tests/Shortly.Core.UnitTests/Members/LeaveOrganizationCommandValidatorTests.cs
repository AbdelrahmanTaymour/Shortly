using FluentValidation.TestHelper;
using Shortly.Core.Members.LeaveOrganization;

namespace Shortly.Core.UnitTests.Members;

public class LeaveOrganizationCommandValidatorTests
{
    private readonly LeaveOrganizationCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAnOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new LeaveOrganizationCommand(Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new LeaveOrganizationCommand(Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }
}
