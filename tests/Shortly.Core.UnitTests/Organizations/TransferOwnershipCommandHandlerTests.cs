using Shortly.Core.Organizations.TransferOwnership;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;

namespace Shortly.Core.UnitTests.Organizations;

public class TransferOwnershipCommandHandlerTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _newOwnerId = Guid.NewGuid();
    private readonly Organization _organization;
    private readonly OrganizationMember _oldOwnerMembership;
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public TransferOwnershipCommandHandlerTests()
    {
        _organization = OrganizationData.Create(_ownerId);
        _oldOwnerMembership = MemberData.Create(_organization.Id, _ownerId, MemberRole.OrgOwner);
        _organizations.GetByIdAsync(_organization.Id, Arg.Any<CancellationToken>()).Returns(_organization);
        _members.GetByOrganizationAndUserAsync(_organization.Id, _ownerId, Arg.Any<CancellationToken>()).Returns(_oldOwnerMembership);
    }

    private OrganizationMember NewOwnerIs(MemberRole role, bool isActive = true)
    {
        var membership = MemberData.Create(_organization.Id, _newOwnerId, role, isActive);
        _members.GetByOrganizationAndUserAsync(_organization.Id, _newOwnerId, Arg.Any<CancellationToken>()).Returns(membership);
        return membership;
    }

    private Task<Result> Handle(Guid callerId, Guid? newOwnerId = null)
    {
        var sut = new TransferOwnershipCommandHandler(
            CallerData.Create(callerId), _organizations, _members, _unitOfWork, TestClock.Provider());
        return sut.Handle(new TransferOwnershipCommand(_organization.Id, newOwnerId ?? _newOwnerId), default);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheOrganizationDoesNotExist()
    {
        // Arrange
        _organizations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Organization?)null);

        // Act
        var result = await Handle(_ownerId);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotOwner_AndChangeNothing_When_TheCallerIsNotTheOwner()
    {
        // Arrange
        NewOwnerIs(MemberRole.OrgAdmin);

        // Act
        var result = await Handle(Guid.NewGuid());

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotOwner);
        _organization.OwnerId.Should().Be(_ownerId);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNewOwnerNotMember_When_TheRecipientIsNotAMember()
    {
        // Act
        var result = await Handle(_ownerId);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NewOwnerNotMember);
        _organization.OwnerId.Should().Be(_ownerId);
    }

    [Fact]
    public async Task Handle_Should_ReturnNewOwnerNotMember_When_TheRecipientWasRemoved()
    {
        // Arrange
        NewOwnerIs(MemberRole.Member, isActive: false);

        // Act
        var result = await Handle(_ownerId);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NewOwnerNotMember);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheCurrentOwnersMembershipIsMissing()
    {
        // Arrange
        NewOwnerIs(MemberRole.Member);
        _members.GetByOrganizationAndUserAsync(_organization.Id, _ownerId, Arg.Any<CancellationToken>()).Returns((OrganizationMember?)null);

        // Act
        var result = await Handle(_ownerId);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.NotFound);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    [InlineData(MemberRole.OrgAdmin)]
    public async Task Handle_Should_MakeTheRecipientTheOwner_AndTheOldOwnerAMember_WhateverTheRecipientsRoleWas(MemberRole recipientRole)
    {
        // Arrange
        var newOwnerMembership = NewOwnerIs(recipientRole);

        // Act
        var result = await Handle(_ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _organization.OwnerId.Should().Be(_newOwnerId);
        newOwnerMembership.Role.Should().Be(MemberRole.OrgOwner);
        _oldOwnerMembership.Role.Should().Be(MemberRole.Member);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_KeepExactlyOneOwner_AfterTheTransfer()
    {
        // Arrange
        var newOwnerMembership = NewOwnerIs(MemberRole.OrgAdmin);

        // Act
        await Handle(_ownerId);

        // Assert
        new[] { _oldOwnerMembership, newOwnerMembership }.Count(m => m.Role == MemberRole.OrgOwner).Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyOwner_AndSaveNothing_When_TheOwnerTransfersToThemselves()
    {
        // Act
        var result = await Handle(_ownerId, newOwnerId: _ownerId);

        // Assert
        result.Error.Should().Be(OrganizationErrors.AlreadyOwner);
        _oldOwnerMembership.Role.Should().Be(MemberRole.OrgOwner);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
