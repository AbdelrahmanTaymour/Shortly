using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;

namespace Shortly.Api.FunctionalTests;

/// <summary>Invitations keep their history: they end up Accepted, Expired or Cancelled and are never deleted.</summary>
[Collection(ApiCollection.Name)]
public class InvitationLifecycleTests(ApiFixture api) : ApiTestBase(api)
{
    private const string Email = "invitee@example.com";

    private async Task<T> WithRepository<T>(Func<IOrganizationInvitationRepository, Task<T>> action)
    {
        using var scope = Api.Default.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<IOrganizationInvitationRepository>());
    }

    private async Task<(OrganizationFixture Org, HttpClient Owner, Guid InvitationId)> PendingInvitationAsync(string email = Email)
    {
        var org = await CreateOrganizationAsync();
        await Seed.CreateInvitationAsync(org.OrganizationId, email, MemberRole.Member, org.OwnerId);
        var invitation = (await Seed.GetInvitationAsync(org.OrganizationId, email))!;
        return (org, As(org.OwnerId, organizationId: org.OrganizationId), invitation.Id);
    }

    // ---- cancelling --------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Cancel_KeepsTheInvitationRecord_AndMarksItCancelled()
    {
        var (org, owner, invitationId) = await PendingInvitationAsync();

        var response = await owner.DeleteAsync($"/api/organizations/{org.OrganizationId}/invitations/{invitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var state = await Seed.GetInvitationAsync(org.OrganizationId, Email);
        state.Should().NotBeNull();
        state!.Status.Should().Be(enInvitationStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_Fails_ForAnAcceptedInvitation()
    {
        var (org, owner, invitationId) = await PendingInvitationAsync();
        await Seed.AcceptInvitationAsync(invitationId);

        var response = await owner.DeleteAsync($"/api/organizations/{org.OrganizationId}/invitations/{invitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Seed.GetInvitationAsync(org.OrganizationId, Email))!.Status.Should().Be(enInvitationStatus.Accepted);
    }

    [Fact]
    public async Task Cancel_Fails_ForAnInvitationThatReachedItsExpiry()
    {
        var (org, owner, invitationId) = await PendingInvitationAsync();
        await Seed.SetInvitationExpiryAsync(invitationId, DateTime.UtcNow.AddMinutes(-1));

        var response = await owner.DeleteAsync($"/api/organizations/{org.OrganizationId}/invitations/{invitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Seed.GetInvitationAsync(org.OrganizationId, Email))!.Status.Should().Be(enInvitationStatus.Pending);
    }

    [Fact]
    public async Task Cancel_Fails_WhenCalledTwice()
    {
        var (org, owner, invitationId) = await PendingInvitationAsync();
        await owner.DeleteAsync($"/api/organizations/{org.OrganizationId}/invitations/{invitationId}");

        var response = await owner.DeleteAsync($"/api/organizations/{org.OrganizationId}/invitations/{invitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ANewInvitation_CanBeCreated_AfterTheEarlierOneWasCancelled()
    {
        var (org, owner, invitationId) = await PendingInvitationAsync();
        await owner.DeleteAsync($"/api/organizations/{org.OrganizationId}/invitations/{invitationId}");

        var hasPending = await WithRepository(r => r.HasPendingInvitationAsync(Email, org.OrganizationId, DateTime.UtcNow));

        hasPending.Should().BeFalse();
    }

    // ---- expiry ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task HasPendingInvitation_IsFalse_OnceTheExpiryInstantIsReached()
    {
        var (org, _, invitationId) = await PendingInvitationAsync();
        var expiry = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await Seed.SetInvitationExpiryAsync(invitationId, expiry);

        var justBefore = await WithRepository(r => r.HasPendingInvitationAsync(Email, org.OrganizationId, expiry.AddTicks(-1)));
        var atExpiry = await WithRepository(r => r.HasPendingInvitationAsync(Email, org.OrganizationId, expiry));

        justBefore.Should().BeTrue();
        atExpiry.Should().BeFalse();
    }

    [Fact]
    public async Task TheCleanup_MarksOverduePendingInvitationsExpired_AndLeavesTheOthersAlone()
    {
        var (org, _, overdueId) = await PendingInvitationAsync("overdue@example.com");
        await Seed.CreateInvitationAsync(org.OrganizationId, "current@example.com", MemberRole.Member, org.OwnerId);
        await Seed.CreateInvitationAsync(org.OrganizationId, "accepted@example.com", MemberRole.Member, org.OwnerId);
        var acceptedId = (await Seed.GetInvitationAsync(org.OrganizationId, "accepted@example.com"))!.Id;
        await Seed.AcceptInvitationAsync(acceptedId);
        await Seed.SetInvitationExpiryAsync(acceptedId, DateTime.UtcNow.AddDays(-30));
        await Seed.SetInvitationExpiryAsync(overdueId, DateTime.UtcNow.AddDays(-1));

        var changed = await WithRepository(r => r.MarkOverdueInvitationsExpiredAsync(DateTime.UtcNow));

        changed.Should().BeGreaterThanOrEqualTo(1);
        (await Seed.GetInvitationAsync(org.OrganizationId, "overdue@example.com"))!.Status.Should().Be(enInvitationStatus.Expired);
        (await Seed.GetInvitationAsync(org.OrganizationId, "current@example.com"))!.Status.Should().Be(enInvitationStatus.Pending);
        (await Seed.GetInvitationAsync(org.OrganizationId, "accepted@example.com"))!.Status.Should().Be(enInvitationStatus.Accepted);
    }

    // ---- resending ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Resend_MakesAnExpiredInvitationPendingAgain_WithANewExpiry()
    {
        var (org, owner, invitationId) = await PendingInvitationAsync();
        await Seed.SetInvitationExpiryAsync(invitationId, DateTime.UtcNow.AddDays(-2));
        await WithRepository(r => r.MarkOverdueInvitationsExpiredAsync(DateTime.UtcNow));
        (await Seed.GetInvitationAsync(org.OrganizationId, Email))!.Status.Should().Be(enInvitationStatus.Expired);

        var response = await owner.PostAsync($"/api/organizations/{org.OrganizationId}/invitations/{invitationId}/resend", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        var state = (await Seed.GetInvitationAsync(org.OrganizationId, Email))!;
        state.Status.Should().Be(enInvitationStatus.Pending);
        state.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Resend_Fails_ForACancelledInvitation()
    {
        var (org, owner, invitationId) = await PendingInvitationAsync();
        await owner.DeleteAsync($"/api/organizations/{org.OrganizationId}/invitations/{invitationId}");

        var response = await owner.PostAsync($"/api/organizations/{org.OrganizationId}/invitations/{invitationId}/resend", null);

        response.IsSuccessStatusCode.Should().BeFalse();
        (await Seed.GetInvitationAsync(org.OrganizationId, Email))!.Status.Should().Be(enInvitationStatus.Cancelled);
    }
}
