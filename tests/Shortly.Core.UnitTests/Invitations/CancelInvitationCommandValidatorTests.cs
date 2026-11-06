using FluentValidation.TestHelper;
using Shortly.Core.Invitations.CancelInvitation;

namespace Shortly.Core.UnitTests.Invitations;

public class CancelInvitationCommandValidatorTests
{
    private readonly CancelInvitationCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new CancelInvitationCommand(Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new CancelInvitationCommand(Guid.Empty, Guid.NewGuid()));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InInvitationId()
    {
        // Act
        var result = _sut.TestValidate(new CancelInvitationCommand(Guid.NewGuid(), Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InvitationId);
    }
}
