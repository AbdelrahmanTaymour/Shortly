using FluentValidation.TestHelper;
using Shortly.Core.Invitations.RejectInvitation;

namespace Shortly.Core.UnitTests.Invitations;

public class RejectInvitationCommandValidatorTests
{
    private readonly RejectInvitationCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForANonBlankToken()
    {
        // Act
        var result = _sut.TestValidate(new RejectInvitationCommand("token"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_ForABlankToken(string token)
    {
        // Act
        var result = _sut.TestValidate(new RejectInvitationCommand(token));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Theory]
    [InlineData(2048, true)]
    [InlineData(2049, false)]
    public void Validate_Should_EnforceTheTokenLength(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(new RejectInvitationCommand(new string('t', length)));

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.Token);
        else result.ShouldHaveValidationErrorFor(x => x.Token);
    }
}
