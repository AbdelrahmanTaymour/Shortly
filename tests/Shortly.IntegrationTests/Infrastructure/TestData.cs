using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Domain.ClickEvents;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.RefreshToken;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Teams;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.IntegrationTests.Infrastructure;

/// <summary>
/// Arranges data by writing it straight to the database (no application code involved) with unique values,
/// so tests sharing one database never collide.
/// </summary>
public sealed class TestData(IntegrationTestWebAppFactory factory)
{
    public static DateTime Now => DateTime.UtcNow;

    public sealed record OrgFixture(Organization Organization, User Owner, OrganizationMember OwnerMember)
    {
        public Guid Id => Organization.Id;
    }

    public async Task<User> UserAsync(
        bool isActive = true, bool emailConfirmed = true, enSubscriptionPlan plan = enSubscriptionPlan.Free,
        string? email = null, string? username = null)
    {
        var unique = Guid.NewGuid().ToString("N");
        var user = User.Register(email ?? $"{unique}@example.com", username ?? $"user{unique}", "not-a-real-hash", Now).Value;
        if (emailConfirmed) user.ConfirmEmail(Now);
        if (plan != enSubscriptionPlan.Free) user.ChangeSubscriptionPlan(plan, Now);
        if (!isActive) user.Deactivate(Now);
        await SaveAsync(user);
        return user;
    }

    /// <summary>A user whose password is hashed with the real <see cref="IPasswordHasher"/>, so it can be verified or changed.</summary>
    public async Task<User> UserWithPasswordAsync(
        string password, bool isActive = true, bool emailConfirmed = true, string? email = null, string? username = null)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash(password);

        var unique = Guid.NewGuid().ToString("N");
        var user = User.Register(email ?? $"{unique}@example.com", username ?? $"user{unique}", hash, Now).Value;
        if (emailConfirmed) user.ConfirmEmail(Now);
        if (!isActive) user.Deactivate(Now);
        await SaveAsync(user);
        return user;
    }

    public async Task<OrgFixture> OrganizationAsync(
        User? owner = null, enSubscriptionPlan plan = enSubscriptionPlan.Free, int memberLimit = 25, string? name = null)
    {
        owner ??= await UserAsync();
        var organization = Organization.Create(
            name ?? $"Org {Guid.NewGuid():N}", null, null, null, memberLimit, owner.Id, plan, Now).Value;
        var ownerMember = OrganizationMember.CreateOwner(organization.Id, owner.Id, Now);
        await SaveAsync(organization, ownerMember);
        return new OrgFixture(organization, owner, ownerMember);
    }

    public async Task<(User User, OrganizationMember Member)> MemberAsync(
        Guid organizationId, MemberRole role = MemberRole.Member, bool isActive = true, Guid? invitedBy = null)
    {
        var user = await UserAsync();
        var member = OrganizationMember.Invite(organizationId, user.Id, role, invitedBy ?? Guid.NewGuid(), Now).Value;
        if (!isActive) member.Deactivate();
        await SaveAsync(member);
        return (user, member);
    }

    public async Task<OrganizationTeam> TeamAsync(Guid organizationId, Guid managerMemberId, string? name = null)
    {
        var team = OrganizationTeam.Create(organizationId, managerMemberId, name ?? $"Team {Guid.NewGuid():N}", null, Now).Value;
        await SaveAsync(team, OrganizationTeamMember.Create(team.Id, managerMemberId, Now));
        return team;
    }

    public async Task<OrganizationTeamMember> TeamMemberAsync(Guid teamId, Guid memberId)
    {
        var teamMember = OrganizationTeamMember.Create(teamId, memberId, Now);
        await SaveAsync(teamMember);
        return teamMember;
    }

    public async Task<OrganizationInvitation> InvitationAsync(
        Guid organizationId, string? email = null, MemberRole role = MemberRole.Member, DateTime? expiresAt = null,
        string? tokenHash = null, DateTime? createdAt = null)
    {
        var invitedBy = await OwnerIdAsync(organizationId); // invited_by is a foreign key to users
        var invitation = OrganizationInvitation.Create(
            organizationId, email ?? $"{Guid.NewGuid():N}@example.com", role, invitedBy,
            tokenHash ?? $"hash-{Guid.NewGuid():N}", expiresAt ?? (createdAt ?? Now).AddDays(7), createdAt ?? Now).Value;
        await SaveAsync(invitation);
        return invitation;
    }

    public async Task<ShortUrl> UserLinkAsync(Guid userId, int clickLimit = -1, string? code = null)
    {
        var link = ShortUrl.CreateForUser(userId, Url(), code is null ? Code() : ShortCode.Create(code).Value, Now, clickLimit).Value;
        await SaveAsync(link);
        return link;
    }

    public async Task<ShortUrl> OrganizationLinkAsync(Guid organizationId, Guid memberId, int clickLimit = -1, string? code = null)
    {
        var link = ShortUrl.CreateForOrganization(
            organizationId, memberId, Url(), code is null ? Code() : ShortCode.Create(code).Value, Now, clickLimit).Value;
        await SaveAsync(link);
        return link;
    }

    public async Task<ClickEvent> ClickAsync(
        long shortUrlId, DateTime? at = null, string country = "Egypt", string deviceType = "Desktop", string? campaign = null,
        string? session = null, string city = "Cairo", string browser = "Chrome", string os = "Windows",
        string? referrerDomain = null, string? trafficSource = null, string? source = null, string? medium = null)
    {
        var click = ClickEvent.Record(
            shortUrlId, at ?? Now, "203.0.113.50", session ?? Guid.NewGuid().ToString("N"), "seed-agent",
            null, referrerDomain, trafficSource ?? (campaign is null ? "Direct" : "Campaign"),
            source ?? (campaign is null ? null : "newsletter"), medium ?? (campaign is null ? null : "email"), campaign, null, null,
            country, city, browser, os, "Other", deviceType).Value;
        await SaveAsync(click);
        return click;
    }

    public async Task<RefreshToken> RefreshTokenAsync(
        Guid userId, string? tokenHash = null, TimeSpan? lifetime = null, Guid? organizationId = null)
    {
        var token = RefreshToken.Issue(userId, tokenHash ?? $"rt-{Guid.NewGuid():N}", lifetime ?? TimeSpan.FromDays(15), organizationId, Now);
        await SaveAsync(token);
        return token;
    }

    public async Task<UserActionToken> PasswordResetTokenAsync(Guid userId, string? tokenHash = null, DateTime? issuedAt = null)
    {
        var token = UserActionToken.IssuePasswordReset(userId, tokenHash ?? $"pr-{Guid.NewGuid():N}", issuedAt ?? Now).Value;
        await SaveAsync(token);
        return token;
    }

    private async Task<Guid> OwnerIdAsync(Guid organizationId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Shortly.Infrastructure.ApplicationDbContext>();
        return await db.Set<Organization>().Where(o => o.Id == organizationId).Select(o => o.OwnerId).SingleAsync();
    }

    private static OriginalUrl Url() => OriginalUrl.Create("https://example.com/integration-test").Value;

    // Starts with a letter: an all-digit code would be routed as a numeric id by the API.
    private static ShortCode Code() => ShortCode.Create("t" + Guid.NewGuid().ToString("N")[..7]).Value;

    private async Task SaveAsync(params object[] entities)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Shortly.Infrastructure.ApplicationDbContext>();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }
}
