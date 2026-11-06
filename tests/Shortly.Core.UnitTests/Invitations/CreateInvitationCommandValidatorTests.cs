using FluentValidation.TestHelper;
using Shortly.Core.Invitations.CreateInvitation;
using Shortly.Domain.Members;

namespace Shortly.Core.UnitTests.Invitations;

public class CreateInvitationCommandValidatorTests
{
    private readonly CreateInvitationCommandValidator _sut = new();

    private static CreateInvitationCommand Valid() => new(Guid.NewGuid(), "invitee@example.com", MemberRole.Member);

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(Valid());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { OrganizationId = Guid.Empty });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_Should_Fail_ForABlankOrMalformedEmail(string email)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Email = email });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_Should_Fail_ForAnEmailLongerThan254Characters()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Email = new string('a', 250) + "@example.com" });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_Should_Fail_ForAnUndefinedRole()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Role = (MemberRole)99 });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Validate_Should_NotRejectTheOwnerRole_BecauseTheHandlerAndTheEntityDoThat()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Role = MemberRole.OrgOwner });

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }
}
