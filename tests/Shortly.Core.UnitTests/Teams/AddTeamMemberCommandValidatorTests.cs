using FluentValidation.TestHelper;
using Shortly.Core.Teams.AddTeamMember;

namespace Shortly.Core.UnitTests.Teams;

public class AddTeamMemberCommandValidatorTests
{
    private readonly AddTeamMemberCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new AddTeamMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new AddTeamMemberCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InTeamId()
    {
        // Act
        var result = _sut.TestValidate(new AddTeamMemberCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid()));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TeamId);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InMemberId()
    {
        // Act
        var result = _sut.TestValidate(new AddTeamMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MemberId);
    }
}
