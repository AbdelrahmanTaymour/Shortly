using FluentValidation.TestHelper;
using Shortly.Core.Members.AddMember;
using Shortly.Domain.Members;

namespace Shortly.Core.UnitTests.Members;

public class AddMemberCommandValidatorTests
{
    private readonly AddMemberCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new AddMemberCommand(Guid.NewGuid(), Guid.NewGuid(), MemberRole.Member));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new AddMemberCommand(Guid.Empty, Guid.NewGuid(), MemberRole.Member));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InUserId()
    {
        // Act
        var result = _sut.TestValidate(new AddMemberCommand(Guid.NewGuid(), Guid.Empty, MemberRole.Member));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_Should_Fail_ForAnUndefinedRole_InRole()
    {
        // Act
        var result = _sut.TestValidate(new AddMemberCommand(Guid.NewGuid(), Guid.NewGuid(), (MemberRole)99));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
}
