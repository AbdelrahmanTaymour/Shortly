using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;
using Shortly.Domain.Users;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Infrastructure;

namespace Shortly.Api.FunctionalTests.Infrastructure;

/// <summary>Inserts test data directly through EF Core, bypassing the API.</summary>
public sealed class Seeder(IServiceProvider services)
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;

    public async Task<Guid> CreateUserAsync()
    {
        var unique = Guid.NewGuid().ToString("N");
        var user = User.Register($"{unique}@example.com", $"user{unique}", "not-a-real-hash", UtcNow).Value;

        await WithDb(async db =>
        {
            db.Add(user);
            await db.SaveChangesAsync();
        });
        return user.Id;
    }

    /// <summary>Creates a user that can log in with the given password.</summary>
    public async Task<(Guid UserId, string Email)> CreateUserWithPasswordAsync(string password)
    {
        var unique = Guid.NewGuid().ToString("N");
        var hash = services.GetRequiredService<IPasswordHasher>().Hash(password);
        var user = User.Register($"{unique}@example.com", $"user{unique}", hash, UtcNow).Value;
        user.ConfirmEmail(UtcNow);

        await WithDb(async db =>
        {
            db.Add(user);
            await db.SaveChangesAsync();
        });
        return (user.Id, user.Email);
    }

    public async Task<(int FailedAttempts, DateTime? LockedUntil)> GetLoginSecurityAsync(Guid userId)
    {
        var state = (0, (DateTime?)null);
        await WithDb(async db =>
        {
            var security = await db.Set<UserSecurity>().AsNoTracking().SingleAsync(s => s.UserId == userId);
            state = (security.FailedLoginAttempts, security.LockedUntilUtc);
        });
        return state;
    }

    public Task DeactivateUserAsync(Guid userId) => WithDb(async db =>
    {
        var user = await db.Set<User>().SingleAsync(u => u.Id == userId);
        user.Deactivate(UtcNow).IsSuccess.Should().BeTrue();
        await db.SaveChangesAsync();
    });

    /// <summary>Creates an active organization whose owner is an active OrgOwner member.</summary>
    public async Task<Guid> CreateOrganizationAsync(Guid ownerId, int memberLimit = 25)
    {
        var organization = Organization.Create(
            $"Org {Guid.NewGuid():N}", null, null, null, memberLimit, ownerId, enSubscriptionPlan.Free, UtcNow).Value;

        await WithDb(async db =>
        {
            db.Add(organization);
            db.Add(OrganizationMember.CreateOwner(organization.Id, ownerId, UtcNow));
            await db.SaveChangesAsync();
        });
        return organization.Id;
    }

    public async Task<Guid> AddMemberAsync(Guid organizationId, Guid userId, MemberRole role, bool isActive = true)
    {
        var member = OrganizationMember.Invite(organizationId, userId, role, userId, UtcNow).Value;
        if (!isActive) member.Deactivate();

        await WithDb(async db =>
        {
            db.Add(member);
            await db.SaveChangesAsync();
        });
        return member.Id;
    }

    /// <summary>Soft-removes a member the way the RemoveMember endpoint does (the row stays, IsActive = false).</summary>
    public Task RemoveMemberAsync(Guid organizationId, Guid userId) => WithDb(async db =>
    {
        var member = db.Set<OrganizationMember>()
            .Single(m => m.OrganizationId == organizationId && m.UserId == userId);
        member.Deactivate();
        await db.SaveChangesAsync();
    });

    public async Task<long> CreateUserLinkAsync(Guid userId, int clickLimit = -1, string? shortCode = null)
    {
        var link = ShortUrl.CreateForUser(userId, Url(), shortCode is null ? Code() : ShortCode.Create(shortCode).Value, UtcNow, clickLimit).Value;
        await WithDb(async db =>
        {
            db.Add(link);
            await db.SaveChangesAsync();
        });
        return link.Id;
    }

    public async Task<long> CreateOrganizationLinkAsync(Guid organizationId, Guid memberId, int clickLimit = -1, string? shortCode = null)
    {
        var link = ShortUrl.CreateForOrganization(organizationId, memberId, Url(), shortCode is null ? Code() : ShortCode.Create(shortCode).Value, UtcNow, clickLimit).Value;
        await WithDb(async db =>
        {
            db.Add(link);
            await db.SaveChangesAsync();
        });
        return link.Id;
    }

    public async Task<Guid> GetMemberIdAsync(Guid organizationId, Guid userId)
    {
        var id = Guid.Empty;
        await WithDb(db =>
        {
            id = db.Set<OrganizationMember>().Single(m => m.OrganizationId == organizationId && m.UserId == userId).Id;
            return Task.CompletedTask;
        });
        return id;
    }

    /// <summary>A link created by an anonymous visitor (no user, no organization).</summary>
    public async Task<long> CreateAnonymousLinkAsync()
    {
        var link = ShortUrl.CreateForAnonymous(Guid.NewGuid().ToString("N"), "203.0.113.9", Url(), Code(), UtcNow).Value;
        await WithDb(async db =>
        {
            db.Add(link);
            await db.SaveChangesAsync();
        });
        return link.Id;
    }

    public async Task<bool> LinkExistsAsync(long shortUrlId)
    {
        var exists = false;
        await WithDb(db =>
        {
            exists = db.Set<ShortUrl>().Any(s => s.Id == shortUrlId);
            return Task.CompletedTask;
        });
        return exists;
    }

    public async Task<bool> OrganizationIsActiveAsync(Guid organizationId)
    {
        var active = false;
        await WithDb(db =>
        {
            active = db.Set<Organization>().Single(o => o.Id == organizationId).IsActive;
            return Task.CompletedTask;
        });
        return active;
    }

    public sealed record LinkState(
        long Id, string ShortCode, string OriginalUrl, bool IsActive, DateTime? ExpiresAt, bool IsPasswordProtected);

    public async Task<LinkState?> GetLinkAsync(long shortUrlId)
    {
        LinkState? state = null;
        await WithDb(db =>
        {
            var link = db.Set<ShortUrl>().SingleOrDefault(s => s.Id == shortUrlId);
            state = link is null
                ? null
                : new LinkState(link.Id, link.ShortCode.Value, link.OriginalUrl.Value, link.IsActive, link.ExpiresAt, link.IsPasswordProtected);
            return Task.CompletedTask;
        });
        return state;
    }

    public async Task<int> CountOrganizationLinksAsync(Guid organizationId)
    {
        var count = 0;
        await WithDb(db =>
        {
            count = db.Set<ShortUrl>().Count(s => s.OrganizationId == organizationId);
            return Task.CompletedTask;
        });
        return count;
    }

    public async Task<int> CountUserLinksAsync(Guid userId)
    {
        var count = 0;
        await WithDb(db =>
        {
            count = db.Set<ShortUrl>().Count(s => s.UserId == userId);
            return Task.CompletedTask;
        });
        return count;
    }

    // ---- plans and usage ----------------------------------------------------------------------

    public Task SetUserPlanAsync(Guid userId, enSubscriptionPlan plan) => WithDb(async db =>
    {
        db.Set<User>().Single(u => u.Id == userId).ChangeSubscriptionPlan(plan, UtcNow);
        await db.SaveChangesAsync();
    });

    public Task SetOrganizationPlanAsync(Guid organizationId, enSubscriptionPlan plan) => WithDb(async db =>
    {
        db.Set<Organization>().Single(o => o.Id == organizationId).ChangeSubscriptionPlan(plan, UtcNow);
        await db.SaveChangesAsync();
    });

    public async Task<(enSubscriptionPlan Plan, bool IsSubscribed)> GetOrganizationPlanAsync(Guid organizationId)
    {
        (enSubscriptionPlan, bool) result = default;
        await WithDb(db =>
        {
            var organization = db.Set<Organization>().Single(o => o.Id == organizationId);
            result = (organization.SubscriptionPlanId, organization.IsSubscribed);
            return Task.CompletedTask;
        });
        return result;
    }

    public async Task<enSubscriptionPlan> GetUserPlanAsync(Guid userId)
    {
        var plan = enSubscriptionPlan.Free;
        await WithDb(db =>
        {
            plan = db.Set<User>().Single(u => u.Id == userId).SubscriptionPlanId;
            return Task.CompletedTask;
        });
        return plan;
    }

    /// <summary>Sets the user's "links created this month" counter to <paramref name="count"/>.</summary>
    public Task SetUserMonthlyLinksAsync(Guid userId, int count) => WithDb(async db =>
    {
        var user = db.Set<User>().Include(u => u.Usage).Single(u => u.Id == userId);
        for (var i = 0; i < count; i++) user.Usage.IncrementLinksCreated(UtcNow);
        await db.SaveChangesAsync();
    });

    public Task SetOrganizationMonthlyLinksAsync(Guid organizationId, int count) => WithDb(async db =>
    {
        var organization = db.Set<Organization>().Include(o => o.Usage).Single(o => o.Id == organizationId);
        for (var i = 0; i < count; i++) organization.Usage.IncrementLinksCreated(UtcNow);
        await db.SaveChangesAsync();
    });

    public async Task<int> GetUserMonthlyLinksAsync(Guid userId)
    {
        var count = 0;
        await WithDb(db =>
        {
            count = db.Set<User>().Include(u => u.Usage).Single(u => u.Id == userId).Usage.MonthlyLinksCreated;
            return Task.CompletedTask;
        });
        return count;
    }

    public async Task<int> GetOrganizationMonthlyLinksAsync(Guid organizationId)
    {
        var count = 0;
        await WithDb(db =>
        {
            count = db.Set<Organization>().Include(o => o.Usage).Single(o => o.Id == organizationId).Usage.MonthlyLinksCreated;
            return Task.CompletedTask;
        });
        return count;
    }

    // ---- membership state and invitations ------------------------------------------------------

    public sealed record MemberState(Guid Id, bool IsActive, MemberRole Role, DateTime JoinedAt, Guid InvitedBy);

    public async Task<MemberState?> GetMemberStateAsync(Guid organizationId, Guid userId)
    {
        MemberState? state = null;
        await WithDb(db =>
        {
            state = db.Set<OrganizationMember>()
                .Where(m => m.OrganizationId == organizationId && m.UserId == userId)
                .Select(m => new MemberState(m.Id, m.IsActive, m.Role, m.JoinedAt, m.InvitedBy))
                .SingleOrDefault();
            return Task.CompletedTask;
        });
        return state;
    }

    /// <summary>Changes a member's role directly in the database (no token is reissued).</summary>
    public Task SetMemberRoleAsync(Guid organizationId, Guid userId, MemberRole role) => WithDb(async db =>
    {
        var member = db.Set<OrganizationMember>().Single(m => m.OrganizationId == organizationId && m.UserId == userId);
        member.ChangeRole(role);
        await db.SaveChangesAsync();
    });

    public async Task<int> CountMembershipRowsAsync(Guid organizationId, Guid userId)
    {
        var count = 0;
        await WithDb(db =>
        {
            count = db.Set<OrganizationMember>().Count(m => m.OrganizationId == organizationId && m.UserId == userId);
            return Task.CompletedTask;
        });
        return count;
    }

    public async Task<string> GetUserEmailAsync(Guid userId)
    {
        var email = "";
        await WithDb(db =>
        {
            email = db.Set<User>().Single(u => u.Id == userId).Email;
            return Task.CompletedTask;
        });
        return email;
    }

    /// <summary>Creates a pending invitation and returns the plain token the invitee would receive by email.</summary>
    public async Task<string> CreateInvitationAsync(Guid organizationId, string email, MemberRole role, Guid invitedBy)
    {
        var hasher = services.GetRequiredService<ITokenHasher>();
        var token = hasher.GenerateToken();
        var invitation = OrganizationInvitation.Create(
            organizationId, email, role, invitedBy, hasher.Hash(token), UtcNow.AddDays(7), UtcNow).Value;

        await WithDb(async db =>
        {
            db.Add(invitation);
            await db.SaveChangesAsync();
        });
        return token;
    }

    public sealed record InvitationState(Guid Id, enInvitationStatus Status, DateTime ExpiresAt);

    public async Task<InvitationState?> GetInvitationAsync(Guid organizationId, string email)
    {
        InvitationState? state = null;
        await WithDb(db =>
        {
            state = db.Set<OrganizationInvitation>()
                .AsNoTracking()
                .Where(i => i.OrganizationId == organizationId && i.InvitedUserEmail == email)
                .Select(i => new InvitationState(i.Id, i.Status, i.ExpiresAt))
                .SingleOrDefault();
            return Task.CompletedTask;
        });
        return state;
    }

    /// <summary>Moves an invitation's expiry (the domain never allows creating one that is already expired).</summary>
    public Task SetInvitationExpiryAsync(Guid invitationId, DateTime expiresAt) => WithDb(async db =>
        await db.Set<OrganizationInvitation>().Where(i => i.Id == invitationId)
            .ExecuteUpdateAsync(set => set.SetProperty(i => i.ExpiresAt, expiresAt)));

    public Task AcceptInvitationAsync(Guid invitationId) => WithDb(async db =>
    {
        var invitation = await db.Set<OrganizationInvitation>().SingleAsync(i => i.Id == invitationId);
        invitation.Accept(UtcNow).IsSuccess.Should().BeTrue();
        await db.SaveChangesAsync();
    });

    public async Task<bool> InvitationIsPendingAsync(Guid organizationId, string email)
    {
        var pending = false;
        await WithDb(db =>
        {
            pending = db.Set<OrganizationInvitation>()
                .Any(i => i.OrganizationId == organizationId && i.InvitedUserEmail == email
                          && i.Status == enInvitationStatus.Pending);
            return Task.CompletedTask;
        });
        return pending;
    }

    public async Task<Guid> CreateTeamAsync(Guid organizationId, Guid managerMemberId)
    {
        var team = Shortly.Domain.Teams.OrganizationTeam.Create(
            organizationId, managerMemberId, $"Team {Guid.NewGuid():N}", null, UtcNow).Value;
        await WithDb(async db =>
        {
            db.Add(team);
            db.Add(Shortly.Domain.Teams.OrganizationTeamMember.Create(team.Id, managerMemberId, UtcNow));
            await db.SaveChangesAsync();
        });
        return team.Id;
    }

    public async Task<bool> IsTeamMemberAsync(Guid teamId, Guid memberId)
    {
        var exists = false;
        await WithDb(db =>
        {
            exists = db.Set<Shortly.Domain.Teams.OrganizationTeamMember>().Any(t => t.TeamId == teamId && t.MemberId == memberId);
            return Task.CompletedTask;
        });
        return exists;
    }

    public async Task<Guid> GetTeamManagerAsync(Guid teamId)
    {
        var manager = Guid.Empty;
        await WithDb(db =>
        {
            manager = db.Set<Shortly.Domain.Teams.OrganizationTeam>().Single(t => t.Id == teamId).TeamManagerId;
            return Task.CompletedTask;
        });
        return manager;
    }

    // ---- click events, counters and plan rows ---------------------------------------------------

    /// <summary>Inserts a recorded click directly (the background pipeline is bypassed).</summary>
    public async Task AddClickAsync(long shortUrlId, DateTime clickedAtUtc, string? sessionId = null,
        string country = "Egypt", string deviceType = "Desktop", string? utmCampaign = null)
    {
        var click = Shortly.Domain.ClickEvents.ClickEvent.Record(
            shortUrlId, clickedAtUtc, "203.0.113.50", sessionId ?? Guid.NewGuid().ToString("N"), "seed-agent",
            null, null, utmCampaign is null ? "Direct" : "Campaign",
            utmCampaign is null ? null : "newsletter", utmCampaign is null ? null : "email", utmCampaign, null, null,
            country, "Cairo", "Chrome", "Windows", "Other", deviceType).Value;

        await WithDb(async db =>
        {
            db.Add(click);
            await db.SaveChangesAsync();
        });
    }

    public async Task<int> CountClickEventsAsync(long shortUrlId)
    {
        var count = 0;
        await WithDb(db =>
        {
            count = db.Set<Shortly.Domain.ClickEvents.ClickEvent>().Count(c => c.ShortUrlId == shortUrlId);
            return Task.CompletedTask;
        });
        return count;
    }

    public sealed record ClickState(
        string SessionId, string UserAgent, string? Referrer, string? ReferrerDomain,
        string? UtmSource, string? UtmMedium, string? UtmCampaign, string? UtmTerm, string? UtmContent);

    /// <summary>Waits for the background click pipeline to persist <paramref name="expected"/> clicks (or gives up after ~8 s).</summary>
    public async Task<List<ClickState>> WaitForClicksAsync(long shortUrlId, int expected = 1)
    {
        var clicks = new List<ClickState>();
        for (var attempt = 0; attempt < 80; attempt++)
        {
            await WithDb(db =>
            {
                clicks = db.Set<Shortly.Domain.ClickEvents.ClickEvent>().Where(c => c.ShortUrlId == shortUrlId)
                    .Select(c => new ClickState(c.SessionId, c.UserAgent, c.Referrer, c.ReferrerDomain,
                        c.UtmSource, c.UtmMedium, c.UtmCampaign, c.UtmTerm, c.UtmContent)).ToList();
                return Task.CompletedTask;
            });
            if (clicks.Count >= expected) break;
            await Task.Delay(100);
        }
        return clicks;
    }

    /// <summary>Raises the link's basic click counter (the counter, not click events).</summary>
    public Task SetTotalClicksAsync(long shortUrlId, int clicks) => WithDb(async db =>
    {
        var link = db.Set<ShortUrl>().Single(s => s.Id == shortUrlId);
        for (var i = 0; i < clicks; i++) link.RecordClick(UtcNow);
        await db.SaveChangesAsync();
    });

    public async Task<int> GetPlanRetentionAsync(enSubscriptionPlan plan)
    {
        var days = 0;
        await WithDb(db =>
        {
            days = db.Database.SqlQuery<int>(
                $"SELECT click_data_retention_days AS \"Value\" FROM subscription_plans WHERE id = {(short)plan}").Single();
            return Task.CompletedTask;
        });
        return days;
    }

    /// <summary>Changes a plan row's retention. Callers must restore it; plan limits are cached per host for 6 hours.</summary>
    public Task SetPlanRetentionAsync(enSubscriptionPlan plan, int days) => WithDb(async db =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE subscription_plans SET click_data_retention_days = {days} WHERE id = {(short)plan}"));

    // ---- organization status ------------------------------------------------------------------

    public Task DeactivateOrganizationAsync(Guid organizationId) => WithDb(async db =>
    {
        db.Set<Organization>().Single(o => o.Id == organizationId).Deactivate(UtcNow);
        await db.SaveChangesAsync();
    });

    public Task SoftDeleteOrganizationAsync(Guid organizationId) => WithDb(async db =>
    {
        db.Set<Organization>().Single(o => o.Id == organizationId).SoftDelete(UtcNow);
        await db.SaveChangesAsync();
    });

    private static OriginalUrl Url() => OriginalUrl.Create("https://example.com/functional-test").Value;

    // Starts with a letter: an all-digit code would be routed to GET /api/short-urls/{id:long} instead of {shortCode}.
    private static ShortCode Code() =>
        ShortCode.Create("t" + Guid.NewGuid().ToString("N")[..7]).Value;

    private async Task WithDb(Func<ApplicationDbContext, Task> action)
    {
        using var scope = services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }
}
