using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Invitations.ValidateInvitationToken;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Invitations;

namespace Shortly.Core.UnitTests.Invitations;

public class ValidateInvitationTokenQueryHandlerTests
{
    private static readonly ValidateInvitationTokenQuery Query = new("plain-token");

    private readonly IOrganizationInvitationRepository _invitations = Substitute.For<IOrganizationInvitationRepository>();
    private readonly ValidateInvitationTokenQueryHandler _sut;

    public ValidateInvitationTokenQueryHandlerTests()
    {
        var tokenHasher = Substitute.For<ITokenHasher>();
        tokenHasher.Hash(Query.Token).Returns(InvitationData.TokenHash);
        _sut = new ValidateInvitationTokenQueryHandler(_invitations, tokenHasher, TestClock.Provider());
    }

    private OrganizationInvitation InvitationInTheRepository(DateTime? expiresAt = null)
    {
        var invitation = InvitationData.Pending(Guid.NewGuid(), expiresAt: expiresAt);
        _invitations.GetByTokenHashAsync(InvitationData.TokenHash, Arg.Any<CancellationToken>()).Returns(invitation);
        return invitation;
    }

    [Fact]
    public async Task Handle_Should_ReturnTrue_ForAPendingInvitationThatHasNotExpired()
    {
        // Arrange
        InvitationInTheRepository();

        // Act
        var result = await _sut.Handle(Query, default);

        // Assert
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnFalse_WithoutAnError_When_NoInvitationMatches()
    {
        // Act
        var result = await _sut.Handle(Query, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnFalse_AtTheExactExpiryInstant()
    {
        // Arrange
        InvitationInTheRepository(expiresAt: TestClock.UtcNow);

        // Act
        var result = await _sut.Handle(Query, default);

        // Assert
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnFalse_ForAnAcceptedOrCancelledInvitation()
    {
        // Arrange
        var accepted = InvitationInTheRepository();
        accepted.Accept(TestClock.UtcNow);

        // Act
        var acceptedResult = await _sut.Handle(Query, default);
        var cancelled = InvitationInTheRepository();
        cancelled.Cancel(TestClock.UtcNow);
        var cancelledResult = await _sut.Handle(Query, default);

        // Assert
        acceptedResult.Value.Should().BeFalse();
        cancelledResult.Value.Should().BeFalse();
    }
}
