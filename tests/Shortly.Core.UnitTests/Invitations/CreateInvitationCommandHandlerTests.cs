using Microsoft.Extensions.Configuration;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Invitations.CreateInvitation;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Invitations;

public class CreateInvitationCommandHandlerTests
{
    private const string InviteeEmail = "invitee@example.com";

    private readonly Guid _callerId = Guid.NewGuid();
    private readonly Organization _organization = OrganizationData.Create(name: "Acme", memberLimit: 3);
    private readonly AuthorizationData _authorization = new();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly IOrganizationInvitationRepository _invitations = Substitute.For<IOrganizationInvitationRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IEmailNotifier _notifier = Substitute.For<IEmailNotifier>();
    private readonly ITokenHasher _tokenHasher = Substitute.For<ITokenHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Guid _orgId;
    private IConfiguration _configuration = new ConfigurationBuilder().Build();

    public CreateInvitationCommandHandlerTests()
    {
        _orgId = _organization.Id;
        _organizations.GetByIdAsync(_orgId, Arg.Any<CancellationToken>()).Returns(_organization);
        _authorization.CallerIs(_orgId, _callerId, MemberRole.OrgAdmin);
        _tokenHasher.GenerateToken().Returns("plain-token");
        _tokenHasher.Hash("plain-token").Returns("hashed-token");
    }

    private CreateInvitationCommandHandler CreateSut() => new(
        CallerData.Create(_callerId), _authorization.Service, _organizations, _members, _invitations, _users,
        _notifier, _configuration, _tokenHasher, _unitOfWork, TestClock.Provider());

    private CreateInvitationCommand Command(MemberRole role = MemberRole.Member, string email = InviteeEmail) =>
        new(_orgId, email, role);

    // ---- who may invite ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_When_TheCallerIsNotAMember()
    {
        // Arrange
        _authorization.CallerIsNotAMember(_orgId);

        // Act
        var result = await CreateSut().Handle(Command(), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        _invitations.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task Handle_Should_ReturnInsufficientRole_When_TheCallerIsBelowAdmin(MemberRole role)
    {
        // Arrange
        _authorization.CallerIs(_orgId, _callerId, role);

        // Act
        var result = await CreateSut().Handle(Command(), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
    }

    [Fact]
    public async Task Handle_Should_ReturnCannotInviteAsOwner_When_TheOwnerRoleIsRequested()
    {
        // Act
        var result = await CreateSut().Handle(Command(MemberRole.OrgOwner), default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.CannotInviteAsOwner);
        _invitations.DidNotReceiveWithAnyArgs().Add(default!);
    }

    // ---- rules about the invitation ----------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheOrganizationDoesNotExist()
    {
        // Arrange
        _organizations.GetByIdAsync(_orgId, Arg.Any<CancellationToken>()).Returns((Organization?)null);

        // Act
        var result = await CreateSut().Handle(Command(), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnMemberLimitReached_When_TheOrganizationIsFull()
    {
        // Arrange
        _members.GetMemberCountByOrganizationAsync(_orgId, Arg.Any<CancellationToken>()).Returns(3);

        // Act
        var result = await CreateSut().Handle(Command(), default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.MemberLimitReached);
        _invitations.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyPending_When_ThereIsAPendingInvitationForTheEmail()
    {
        // Arrange
        _invitations.HasPendingInvitationAsync(InviteeEmail, _orgId, TestClock.UtcNow, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await CreateSut().Handle(Command(), default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.AlreadyPending);
        _invitations.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyMember_When_TheInvitedEmailBelongsToAnActiveMember()
    {
        // Arrange
        var existingUser = UserData.Create(email: InviteeEmail);
        _users.GetByEmailAsync(InviteeEmail, Arg.Any<CancellationToken>()).Returns(existingUser);
        _members.IsMemberOfOrganizationAsync(existingUser.Id, _orgId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await CreateSut().Handle(Command(), default);

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.AlreadyMember);
    }

    [Fact]
    public async Task Handle_Should_Allow_InvitingSomeoneWithoutAnAccountYet()
    {
        // Arrange
        _users.GetByEmailAsync(InviteeEmail, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await CreateSut().Handle(Command(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Accept_WhenTheLastFreeSlotIsUsed_BecausePendingInvitationsDoNotCount()
    {
        // Arrange
        _members.GetMemberCountByOrganizationAsync(_orgId, Arg.Any<CancellationToken>()).Returns(2);

        // Act
        var result = await CreateSut().Handle(Command(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ---- the created invitation --------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_StoreAPendingInvitation_WithOnlyTheTokenHash_ExpiringInSevenDaysByDefault()
    {
        // Act
        var result = await CreateSut().Handle(Command(MemberRole.Viewer), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _invitations.Received(1).Add(Arg.Is<OrganizationInvitation>(i =>
            i.OrganizationId == _orgId && i.InvitedUserEmail == InviteeEmail && i.InvitedUserRole == MemberRole.Viewer
            && i.InvitedBy == _callerId && i.TokenHash == "hashed-token" && i.Status == enInvitationStatus.Pending
            && i.ExpiresAt == TestClock.UtcNow.AddDays(7)));
    }

    [Fact]
    public async Task Handle_Should_UseTheConfiguredExpiry()
    {
        // Arrange
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AppSettings:Tokens:InvitationExpiryDays"] = "3" })
            .Build();

        // Act
        await CreateSut().Handle(Command(), default);

        // Assert
        _invitations.Received(1).Add(Arg.Is<OrganizationInvitation>(i => i.ExpiresAt == TestClock.UtcNow.AddDays(3)));
    }

    [Fact]
    public async Task Handle_Should_ReturnTheInvitationId_AndEmailThePlainTokenOnlyAfterSaving()
    {
        // Act
        var result = await CreateSut().Handle(Command(), default);

        // Assert
        var createdId = _invitations.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IOrganizationInvitationRepository.Add))
            .Select(c => ((OrganizationInvitation)c.GetArguments()[0]!).Id).Single();
        result.Value.Should().Be(createdId);
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _notifier.EnqueueSendUserInvitation(InviteeEmail, "invitee", "invitee", "plain-token", "Acme");
        });
    }

    [Fact]
    public async Task Handle_Should_NormalizeTheEmail_ForThePendingCheck_AndForStorage()
    {
        // Arrange
        _invitations.HasPendingInvitationAsync(InviteeEmail, _orgId, TestClock.UtcNow, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var duplicate = await CreateSut().Handle(Command(email: "  Invitee@Example.COM "), default);

        // Assert
        duplicate.Error.Should().Be(OrganizationInvitationErrors.AlreadyPending);
    }

    [Fact]
    public async Task Handle_Should_StoreTheNormalizedEmail()
    {
        // Act
        var result = await CreateSut().Handle(Command(email: "  Invitee@Example.COM "), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _invitations.Received(1).Add(Arg.Is<OrganizationInvitation>(i => i.InvitedUserEmail == InviteeEmail));
    }
}
