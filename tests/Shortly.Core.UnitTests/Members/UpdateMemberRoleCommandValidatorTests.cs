using FluentValidation.TestHelper;
using Shortly.Core.Members.UpdateMemberRole;
using Shortly.Domain.Members;

namespace Shortly.Core.UnitTests.Members;

public class UpdateMemberRoleCommandValidatorTests
{
    private readonly UpdateMemberRoleCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new UpdateMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), MemberRole.Member));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new UpdateMemberRoleCommand(Guid.Empty, Guid.NewGuid(), MemberRole.Member));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InUserId()
    {
        // Act
        var result = _sut.TestValidate(new UpdateMemberRoleCommand(Guid.NewGuid(), Guid.Empty, MemberRole.Member));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_Should_Fail_ForAnUndefinedRole_InNewRole()
    {
        // Act
        var result = _sut.TestValidate(new UpdateMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), (MemberRole)99));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewRole);
    }
}
