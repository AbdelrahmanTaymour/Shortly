using Shortly.Core.Organizations.DeleteOrganization;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.UnitTests.Organizations;

public class DeleteOrganizationCommandHandlerTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly ITransaction _transaction = Substitute.For<ITransaction>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public DeleteOrganizationCommandHandlerTests() =>
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);

    private Task<Result> Handle(Organization? organization, Guid callerId, bool isAdmin = false)
    {
        _organizations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(organization);
        var sut = new DeleteOrganizationCommandHandler(
            CallerData.Create(callerId, isAdmin), _organizations, _shortUrls, _unitOfWork, TestClock.Provider());
        return sut.Handle(new DeleteOrganizationCommand(organization?.Id ?? Guid.NewGuid()), default);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheOrganizationDoesNotExist()
    {
        // Act
        var result = await Handle(null, _ownerId);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotOwner_AndDeleteNothing_When_TheCallerIsNotTheOwner()
    {
        // Arrange
        var organization = OrganizationData.Create(_ownerId);

        // Act
        var result = await Handle(organization, Guid.NewGuid());

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotOwner);
        organization.IsDeleted.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotOwner_ForASystemAdminWhoDoesNotOwnTheOrganization()
    {
        // Arrange: only the owner can delete an organization
        var organization = OrganizationData.Create(_ownerId);

        // Act
        var result = await Handle(organization, Guid.NewGuid(), isAdmin: true);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotOwner);
    }

    [Fact]
    public async Task Handle_Should_SoftDeleteTheOrganization_AndDeleteItsLinks()
    {
        // Arrange
        var organization = OrganizationData.Create(_ownerId);

        // Act
        var result = await Handle(organization, _ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organization.IsDeleted.Should().BeTrue();
        organization.DeletedAt.Should().Be(TestClock.UtcNow);
        organization.IsActive.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_AndSaveNothing_When_TheOrganizationIsAlreadyDeleted()
    {
        // Arrange
        var organization = OrganizationData.Create(_ownerId);
        organization.SoftDelete(TestClock.UtcNow);

        // Act
        var result = await Handle(organization, _ownerId);

        // Assert
        result.Error.Should().Be(OrganizationErrors.AlreadyDeleted);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DeleteTheOrganizationLinks_InsideTheTransaction_BeforeCommitting()
    {
        // Arrange
        var organization = OrganizationData.Create(_ownerId);

        // Act
        await Handle(organization, _ownerId);

        // Assert
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _shortUrls.DeleteAllForOrganizationAsync(organization.Id, Arg.Any<CancellationToken>());
            _transaction.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_Should_RollBack_AndRethrow_When_DeletingTheLinksFails()
    {
        // Arrange
        var organization = OrganizationData.Create(_ownerId);
        _shortUrls.DeleteAllForOrganizationAsync(organization.Id, Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("db"));

        // Act
        var act = () => Handle(organization, _ownerId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DeleteNoLinks_When_TheCallerIsNotTheOwner()
    {
        // Act
        await Handle(OrganizationData.Create(_ownerId), Guid.NewGuid());

        // Assert
        await _shortUrls.DidNotReceiveWithAnyArgs().DeleteAllForOrganizationAsync(default, default);
    }
}
