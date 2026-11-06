using Shortly.Core.Organizations.UpdateOrganization;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests.Organizations;

public class UpdateOrganizationCommandHandlerTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private Task<Result> Handle(Organization? organization, UpdateOrganizationCommand command, Guid? callerId = null)
    {
        _organizations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(organization);
        var sut = new UpdateOrganizationCommandHandler(
            CallerData.Create(callerId ?? _ownerId), _organizations, _unitOfWork, TestClock.Provider());
        return sut.Handle(command, default);
    }

    private static UpdateOrganizationCommand Command(
        Guid id, string? name = null, string? description = null, string? website = null, string? logoUrl = null) =>
        new(id, name, description, website, logoUrl);

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheOrganizationDoesNotExist()
    {
        // Act
        var result = await Handle(null, Command(Guid.NewGuid(), name: "New"));

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotOwner_AndChangeNothing_When_TheCallerIsNotTheOwner()
    {
        // Arrange
        var organization = OrganizationData.Create(_ownerId);

        // Act
        var result = await Handle(organization, Command(organization.Id, name: "Hijacked"), callerId: Guid.NewGuid());

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotOwner);
        organization.Name.Should().Be("Acme");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_UpdateTheGivenFields_AndSave()
    {
        // Arrange
        var organization = OrganizationData.Create(_ownerId);

        // Act
        var result = await Handle(organization,
            Command(organization.Id, "  New   name ", "New description", "https://new.dev", "https://new.dev/logo.png"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.Name.Should().Be("New name");
        organization.Description.Should().Be("New description");
        organization.Website.Should().Be("https://new.dev");
        organization.LogoUrl.Should().Be("https://new.dev/logo.png");
        organization.MemberLimit.Should().Be(10);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_LeaveUntouchedFieldsAlone_WhenTheyAreOmitted()
    {
        // Arrange
        var organization = OrganizationData.Create(_ownerId);

        // Act
        await Handle(organization, Command(organization.Id, description: "Only this"));

        // Assert
        organization.Name.Should().Be("Acme");
        organization.MemberLimit.Should().Be(10);
        organization.Description.Should().Be("Only this");
    }

    [Fact]
    public async Task Handle_Should_ClearAnOptionalField_When_ItIsSentAsAnEmptyString()
    {
        // Arrange
        var organization = Organization.Create("Acme", "Description", "https://acme.dev", null, 10, _ownerId,
            Shortly.Domain.SubscriptionPlan.enSubscriptionPlan.Free, TestClock.UtcNow).Value;

        // Act
        await Handle(organization, Command(organization.Id, description: "", website: ""));

        // Assert
        organization.Description.Should().BeNull();
        organization.Website.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnNameRequired_AndSaveNothing_When_TheNameIsBlank()
    {
        // Arrange
        var organization = OrganizationData.Create(_ownerId);

        // Act
        var result = await Handle(organization, Command(organization.Id, name: "   "));

        // Assert
        result.Error.Should().Be(OrganizationErrors.NameRequired);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RefuseAnOrganizationAdmin_WhoIsNotTheOwner()
    {
        // Arrange: only the owner changes the organization name and settings
        var organization = OrganizationData.Create(_ownerId);

        // Act
        var result = await Handle(organization, Command(organization.Id, name: "Renamed by admin"), callerId: Guid.NewGuid());

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotOwner);
        organization.Name.Should().Be("Acme");
    }
}
