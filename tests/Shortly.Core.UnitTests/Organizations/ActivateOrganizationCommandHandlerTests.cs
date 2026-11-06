using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Organizations.ActivateOrganization;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests.Organizations;

public class ActivateOrganizationCommandHandlerTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = TestClock.Provider();

    private Task<Result> Handle(Guid organizationId, Shortly.Core.Abstractions.Authentication.IUserContext caller) =>
        new ActivateOrganizationCommandHandler(_organizations, caller, _unitOfWork, _clock)
            .Handle(new ActivateOrganizationCommand(organizationId), default);

    private Organization OrganizationReturnedByTheRepository(Organization organization)
    {
        _organizations.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);
        return organization;
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheOrganizationDoesNotExist()
    {
        // Arrange
        _organizations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Organization?)null);

        // Act
        var result = await Handle(Guid.NewGuid(), OwnerCallerData.Create(_ownerId));

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ActivateTheOrganization_When_TheCallerIsTheOwner()
    {
        // Arrange
        var organization = OrganizationReturnedByTheRepository(OrganizationData.CreateInactive(_ownerId));

        // Act
        var result = await Handle(organization.Id, OwnerCallerData.Create(_ownerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.IsActive.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ActivateTheOrganization_When_TheCallerIsASystemAdminWhoDoesNotOwnIt()
    {
        // Arrange
        var organization = OrganizationReturnedByTheRepository(OrganizationData.CreateInactive(_ownerId));

        // Act
        var result = await Handle(organization.Id, OwnerCallerData.Create(Guid.NewGuid(), isAdmin: true));

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnNotOwner_AndChangeNothing_When_TheCallerIsAnotherUser()
    {
        // Arrange
        var organization = OrganizationReturnedByTheRepository(OrganizationData.CreateInactive(_ownerId));

        // Act
        var result = await Handle(organization.Id, OwnerCallerData.Create(Guid.NewGuid()));

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotOwner);
        organization.IsActive.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_When_TheOrganizationIsAlreadyActive()
    {
        // Arrange
        var organization = OrganizationReturnedByTheRepository(OrganizationData.Create(_ownerId));

        // Act
        var result = await Handle(organization.Id, OwnerCallerData.Create(_ownerId));

        // Assert
        result.Error.Should().Be(OrganizationErrors.AlreadyActive);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
