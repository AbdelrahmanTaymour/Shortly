using FluentValidation.TestHelper;
using Shortly.Core.Invitations.ResendInvitation;

namespace Shortly.Core.UnitTests.Invitations;

public class ResendInvitationCommandValidatorTests
{
    private readonly ResendInvitationCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new ResendInvitationCommand(Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new ResendInvitationCommand(Guid.Empty, Guid.NewGuid()));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InInvitationId()
    {
        // Act
        var result = _sut.TestValidate(new ResendInvitationCommand(Guid.NewGuid(), Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InvitationId);
    }
}
