using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Invitations.RejectInvitation;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;

namespace Shortly.Core.UnitTests.Invitations;

public class RejectInvitationCommandHandlerTests
{
    private static readonly RejectInvitationCommand Command = new("plain-token");

    private readonly IOrganizationInvitationRepository _invitations = Substitute.For<IOrganizationInvitationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RejectInvitationCommandHandler _sut;

    public RejectInvitationCommandHandlerTests()
    {
        var tokenHasher = Substitute.For<ITokenHasher>();
        tokenHasher.Hash(Command.Token).Returns(InvitationData.TokenHash);
        _sut = new RejectInvitationCommandHandler(_invitations, tokenHasher, _unitOfWork, TestClock.Provider());
    }

    private OrganizationInvitation InvitationInTheRepository(DateTime? expiresAt = null)
    {
        var invitation = InvitationData.Pending(Guid.NewGuid(), expiresAt: expiresAt);
        _invitations.GetByTokenHashAsync(InvitationData.TokenHash, Arg.Any<CancellationToken>()).Returns(invitation);
        return invitation;
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_NoInvitationMatchesTheToken()
    {
        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_CancelTheInvitation_AndSave()
    {
        // Arrange
        var invitation = InvitationInTheRepository();

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(enInvitationStatus.Cancelled);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidOrExpired_AndSaveNothing_When_TheInvitationHasExpired()
    {
        // Arrange
        var invitation = InvitationInTheRepository(expiresAt: TestClock.UtcNow);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidOrExpired);
        invitation.Status.Should().Be(enInvitationStatus.Pending);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidOrExpired_When_TheInvitationWasAlreadyAccepted()
    {
        // Arrange
        var invitation = InvitationInTheRepository();
        invitation.Accept(TestClock.UtcNow);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.InvalidOrExpired);
        invitation.Status.Should().Be(enInvitationStatus.Accepted);
    }
}
