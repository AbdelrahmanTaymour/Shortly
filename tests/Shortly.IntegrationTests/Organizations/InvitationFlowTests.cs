using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Core.Invitations.AcceptInvitation;
using Shortly.Core.Invitations.CancelInvitation;
using Shortly.Core.Invitations.CreateInvitation;
using Shortly.Core.Invitations.GetInvitationById;
using Shortly.Core.Invitations.GetOrganizationInvitations;
using Shortly.Core.Invitations.RejectInvitation;
using Shortly.Core.Invitations.ResendInvitation;
using Shortly.Core.Invitations.ValidateInvitationToken;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Organizations;

public class InvitationFlowTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private string TokenSentTo(string email) => Emails.Sent.Last(e => e.Kind == "Invitation" && e.To == email).Token;

    private Task<OrganizationInvitation> Stored(Guid id) => Query(db => db.Set<OrganizationInvitation>().SingleAsync(i => i.Id == id));

    private async Task<(Guid InvitationId, string Token)> InviteAsync(Guid orgId, string email, MemberRole role = MemberRole.Member)
    {
        var result = await Send(new CreateInvitationCommand(orgId, email, role));
        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Code : "");
        return (result.Value, TokenSentTo(email.Trim().ToLowerInvariant()));
    }

    // ---- creating --------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Create_Should_StoreAPendingInvitationWithAHashedToken_AndEmailThePlainOne()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        As(org.Owner.Id);
        var email = $"{Guid.NewGuid():N}@example.com";

        // Act
        var (id, token) = await InviteAsync(org.Id, email, MemberRole.Viewer);

        // Assert
        var invitation = await Stored(id);
        invitation.Status.Should().Be(enInvitationStatus.Pending);
        invitation.InvitedUserEmail.Should().Be(email);
        invitation.InvitedUserRole.Should().Be(MemberRole.Viewer);
        invitation.InvitedBy.Should().Be(org.Owner.Id);
        invitation.TokenHash.Should().NotBe(token).And.NotBeNullOrWhiteSpace();
        invitation.ExpiresAt.Should().BeCloseTo(TestData.Now.AddDays(7), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Create_Should_NormalizeTheEmail_AndRefuseADuplicatePendingInvitation()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        As(org.Owner.Id);
        var email = $"{Guid.NewGuid():N}@example.com";
        await Send(new CreateInvitationCommand(org.Id, email, MemberRole.Member));

        // Act
        var duplicate = await Send(new CreateInvitationCommand(org.Id, $"  {email.ToUpperInvariant()} ", MemberRole.Member));

        // Assert
        duplicate.Error.Should().Be(OrganizationInvitationErrors.AlreadyPending);
        (await Query(db => db.Set<OrganizationInvitation>().CountAsync(i => i.OrganizationId == org.Id))).Should().Be(1);
    }

    [Fact]
    public async Task Create_Should_Refuse_ANonAdmin_AnOwnerRole_AnExistingMember_AndAFullOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync(memberLimit: 2);
        var (member, _) = await Data.MemberAsync(org.Id); // now full: owner + member
        var roomy = await Data.OrganizationAsync();
        var (existingMember, _) = await Data.MemberAsync(roomy.Id);

        // Act
        As(member.Id);
        var byMember = await Send(new CreateInvitationCommand(org.Id, "x@example.com", MemberRole.Member));
        As(org.Owner.Id);
        var asOwnerRole = await Send(new CreateInvitationCommand(org.Id, "y@example.com", MemberRole.OrgOwner));
        As(roomy.Owner.Id);
        var existing = await Send(new CreateInvitationCommand(roomy.Id, existingMember.Email, MemberRole.Member));
        As(org.Owner.Id);
        var full = await Send(new CreateInvitationCommand(org.Id, "z@example.com", MemberRole.Member));

        // Assert
        byMember.IsFailure.Should().BeTrue();
        asOwnerRole.IsFailure.Should().BeTrue();
        existing.Error.Should().Be(OrganizationInvitationErrors.AlreadyMember);
        full.Error.Should().Be(OrganizationInvitationErrors.MemberLimitReached);
    }

    // ---- accepting -------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Accept_Should_AddAnActiveMemberWithTheInvitedRole_AndConsumeTheInvitation()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var invitee = await Data.UserAsync();
        As(org.Owner.Id);
        var (id, token) = await InviteAsync(org.Id, invitee.Email, MemberRole.OrgAdmin);

        // Act
        As(invitee.Id);
        var result = await Send(new AcceptInvitationCommand(token));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var member = await Query(db => db.Set<OrganizationMember>().SingleAsync(m => m.OrganizationId == org.Id && m.UserId == invitee.Id));
        member.Role.Should().Be(MemberRole.OrgAdmin);
        member.IsActive.Should().BeTrue();
        member.InvitedBy.Should().Be(org.Owner.Id);
        (await Stored(id)).Status.Should().Be(enInvitationStatus.Accepted);
    }

    [Fact]
    public async Task Accept_Should_ReactivateAFormerMember_InsteadOfAddingARow()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (invitee, row) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin, isActive: false);
        As(org.Owner.Id);
        var (_, token) = await InviteAsync(org.Id, invitee.Email, MemberRole.Viewer);

        // Act
        As(invitee.Id);
        await Send(new AcceptInvitationCommand(token));

        // Assert
        var member = await Query(db => db.Set<OrganizationMember>().SingleAsync(m => m.OrganizationId == org.Id && m.UserId == invitee.Id));
        member.Id.Should().Be(row.Id);
        member.IsActive.Should().BeTrue();
        member.Role.Should().Be(MemberRole.Viewer);
    }

    [Fact]
    public async Task Accept_Should_Refuse_AnotherUser_AnUnknownToken_AndAnAlreadyUsedToken()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var invitee = await Data.UserAsync();
        var stranger = await Data.UserAsync();
        As(org.Owner.Id);
        var (_, token) = await InviteAsync(org.Id, invitee.Email);

        // Act
        As(stranger.Id);
        var wrongUser = await Send(new AcceptInvitationCommand(token));
        As(invitee.Id);
        var unknown = await Send(new AcceptInvitationCommand("not-a-token"));
        await Send(new AcceptInvitationCommand(token));
        var again = await Send(new AcceptInvitationCommand(token));

        // Assert
        wrongUser.Error.Should().Be(OrganizationInvitationErrors.EmailMismatch);
        unknown.Error.Should().Be(OrganizationInvitationErrors.NotFound);
        again.IsFailure.Should().BeTrue();
        (await Query(db => db.Set<OrganizationMember>().CountAsync(m => m.OrganizationId == org.Id && m.UserId == invitee.Id))).Should().Be(1);
    }

    [Fact]
    public async Task Accept_Should_Refuse_AnExpiredInvitation_ACancelledOne_AndAnInactiveOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var expiredUser = await Data.UserAsync();
        var cancelledUser = await Data.UserAsync();
        var inactiveUser = await Data.UserAsync();
        As(org.Owner.Id);
        var (expiredId, expiredToken) = await InviteAsync(org.Id, expiredUser.Email);
        var (cancelledId, cancelledToken) = await InviteAsync(org.Id, cancelledUser.Email);
        var (_, inactiveToken) = await InviteAsync(org.Id, inactiveUser.Email);
        await Send(new CancelInvitationCommand(org.Id, cancelledId));
        await Seed(db => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE organization_invitations SET expires_at = {TestData.Now.AddMinutes(-1)} WHERE id = {expiredId}"));

        // Act
        As(expiredUser.Id);
        var expired = await Send(new AcceptInvitationCommand(expiredToken));
        As(cancelledUser.Id);
        var cancelled = await Send(new AcceptInvitationCommand(cancelledToken));
        await Seed(async db => (await db.Set<Organization>().SingleAsync(o => o.Id == org.Id)).Deactivate(TestData.Now));
        As(inactiveUser.Id);
        var inactive = await Send(new AcceptInvitationCommand(inactiveToken));

        // Assert
        expired.IsFailure.Should().BeTrue();
        cancelled.IsFailure.Should().BeTrue();
        inactive.Error.Should().Be(OrganizationErrors.Inactive);
        (await Query(db => db.Set<OrganizationMember>().CountAsync(m => m.OrganizationId == org.Id))).Should().Be(1);
    }

    [Fact]
    public async Task Accept_Should_KeepTheInvitationPending_WhenTheOrganizationIsFull()
    {
        // Arrange
        var org = await Data.OrganizationAsync(memberLimit: 2);
        var invitee = await Data.UserAsync();
        As(org.Owner.Id);
        var (id, token) = await InviteAsync(org.Id, invitee.Email);
        await Data.MemberAsync(org.Id); // takes the last slot after the invitation was sent

        // Act
        As(invitee.Id);
        var result = await Send(new AcceptInvitationCommand(token));

        // Assert
        result.Error.Should().Be(OrganizationInvitationErrors.MemberLimitReached);
        (await Stored(id)).Status.Should().Be(enInvitationStatus.Pending);
    }

    [Fact]
    public async Task Accept_Should_LetExactlyOneOfTheConcurrentRequestsWin_ForTheSameInvitation()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var invitee = await Data.UserAsync();
        As(org.Owner.Id);
        var (_, token) = await InviteAsync(org.Id, invitee.Email);
        As(invitee.Id);

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Send(new AcceptInvitationCommand(token))));

        // Assert
        results.Count(r => r.IsSuccess).Should().Be(1);
        (await Query(db => db.Set<OrganizationMember>().CountAsync(m => m.OrganizationId == org.Id && m.UserId == invitee.Id))).Should().Be(1);
    }

    [Fact]
    public async Task Accept_Should_NeverExceedTheMemberLimit_WhenSeveralInviteesAcceptTogether()
    {
        // Arrange: room for two more members
        var org = await Data.OrganizationAsync(memberLimit: 3);
        var invitees = new List<(User User, string Token)>();
        As(org.Owner.Id);
        for (var i = 0; i < 6; i++)
        {
            var user = await Data.UserAsync();
            var (_, token) = await InviteAsync(org.Id, user.Email);
            invitees.Add((user, token));
        }

        // Act
        var results = await Task.WhenAll(invitees.Select(async i =>
        {
            As(i.User.Id);
            await using var scope = Factory.Services.CreateAsyncScope();
            return await SendAs(i.User.Id, new AcceptInvitationCommand(i.Token));
        }));

        // Assert
        results.Count(r => r.IsSuccess).Should().Be(2);
        (await Query(db => db.Set<OrganizationMember>().CountAsync(m => m.OrganizationId == org.Id && m.IsActive))).Should().Be(3);
    }

    // ---- rejecting, cancelling, resending --------------------------------------------------------------------------------------

    [Fact]
    public async Task Reject_Should_MarkTheInvitationRejected_WithoutAddingAMember()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var invitee = await Data.UserAsync();
        As(org.Owner.Id);
        var (id, token) = await InviteAsync(org.Id, invitee.Email);

        // Act
        As(invitee.Id);
        var result = await Send(new RejectInvitationCommand(token));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Stored(id)).Status.Should().NotBe(enInvitationStatus.Pending);
        (await Query(db => db.Set<OrganizationMember>().CountAsync(m => m.OrganizationId == org.Id && m.UserId == invitee.Id))).Should().Be(0);
    }

    [Fact]
    public async Task Cancel_Should_BeForTheInviterOrAnAdmin_AndMakeTheTokenUseless()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (admin, _) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        var (other, _) = await Data.MemberAsync(org.Id);
        var invitee = await Data.UserAsync();
        As(admin.Id);
        var (id, token) = await InviteAsync(org.Id, invitee.Email);

        // Act
        As(other.Id);
        var byOther = await Send(new CancelInvitationCommand(org.Id, id));
        As(admin.Id);
        var byInviter = await Send(new CancelInvitationCommand(org.Id, id));
        As(invitee.Id);
        var accept = await Send(new AcceptInvitationCommand(token));

        // Assert
        byOther.Error.Should().Be(OrganizationInvitationErrors.NotInviter);
        byInviter.IsSuccess.Should().BeTrue();
        (await Stored(id)).Status.Should().Be(enInvitationStatus.Cancelled);
        accept.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Resend_Should_IssueANewToken_WhichReplacesTheOldOne_AndExtendTheExpiry()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var invitee = await Data.UserAsync();
        As(org.Owner.Id);
        var (id, oldToken) = await InviteAsync(org.Id, invitee.Email);
        await Seed(db => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE organization_invitations SET expires_at = {TestData.Now.AddHours(1)} WHERE id = {id}"));

        // Act
        var resend = await Send(new ResendInvitationCommand(org.Id, id));
        var newToken = TokenSentTo(invitee.Email);
        var oldValid = await Send(new ValidateInvitationTokenQuery(oldToken));
        var newValid = await Send(new ValidateInvitationTokenQuery(newToken));

        // Assert
        resend.IsSuccess.Should().BeTrue();
        newToken.Should().NotBe(oldToken);
        oldValid.Value.Should().BeFalse();
        newValid.Value.Should().BeTrue();
        (await Stored(id)).ExpiresAt.Should().BeAfter(TestData.Now.AddDays(5));
    }

    [Fact]
    public async Task Resend_Should_Refuse_APlainMember_AndAnInvitationOfAnotherOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var other = await Data.OrganizationAsync();
        var (member, _) = await Data.MemberAsync(org.Id);
        As(org.Owner.Id);
        var (id, _) = await InviteAsync(org.Id, $"{Guid.NewGuid():N}@example.com");

        // Act
        As(member.Id);
        var byMember = await Send(new ResendInvitationCommand(org.Id, id));
        As(other.Owner.Id);
        var foreign = await Send(new ResendInvitationCommand(other.Id, id));

        // Assert
        byMember.Error.Should().Be(OrganizationInvitationErrors.NotInviter);
        foreign.Error.Should().Be(OrganizationInvitationErrors.NotFound);
    }

    // ---- validating and reading ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Validate_Should_BeTrueOnlyForALivePendingToken()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        As(org.Owner.Id);
        var (id, token) = await InviteAsync(org.Id, $"{Guid.NewGuid():N}@example.com");

        // Act
        var live = await Send(new ValidateInvitationTokenQuery(token));
        var unknown = await Send(new ValidateInvitationTokenQuery("nope"));
        await Send(new CancelInvitationCommand(org.Id, id));
        var cancelled = await Send(new ValidateInvitationTokenQuery(token));

        // Assert
        live.Value.Should().BeTrue();
        unknown.Value.Should().BeFalse();
        cancelled.Value.Should().BeFalse();
    }

    [Fact]
    public async Task GetInvitations_Should_ListNewestFirst_WithInviterNames_ForAdminsOnly()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (member, _) = await Data.MemberAsync(org.Id);
        var first = await Data.InvitationAsync(org.Id, createdAt: TestData.Now.AddMinutes(-5));
        var second = await Data.InvitationAsync(org.Id);
        await Data.InvitationAsync((await Data.OrganizationAsync()).Id); // another organization's

        // Act
        As(org.Owner.Id);
        var asOwner = (await Send(new GetOrganizationInvitationsQuery(org.Id))).Value;
        var single = (await Send(new GetInvitationByIdQuery(first.Id))).Value;
        As(member.Id);
        var asMember = await Send(new GetOrganizationInvitationsQuery(org.Id));
        var singleAsMember = await Send(new GetInvitationByIdQuery(first.Id));

        // Assert
        asOwner.Items.Select(i => i.Id).Should().Equal(second.Id, first.Id);
        asOwner.TotalCount.Should().Be(2);
        single.InvitedByUsername.Should().Be(org.Owner.Username);
        asMember.IsFailure.Should().BeTrue();
        singleAsMember.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetInvitations_Should_PageTheResults()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        for (var i = 0; i < 5; i++) await Data.InvitationAsync(org.Id);
        As(org.Owner.Id);

        // Act
        var page3 = (await Send(new GetOrganizationInvitationsQuery(org.Id, 3, 2))).Value;

        // Assert
        page3.Items.Should().HaveCount(1);
        page3.TotalPages.Should().Be(3);
    }

    private async Task<Shortly.Domain.Abstractions.Result> SendAs(Guid userId, AcceptInvitationCommand command)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TestUserContext>().Caller = Caller.User(userId);
        return await scope.ServiceProvider.GetRequiredService<MediatR.ISender>().Send(command);
    }
}
