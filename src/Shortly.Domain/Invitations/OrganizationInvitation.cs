using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Domain.Invitations;

public sealed class OrganizationInvitation : Entity<Guid>
{
    private OrganizationInvitation(
        Guid id, Guid organizationId, string invitedUserEmail, MemberRole invitedUserRole,
        Guid invitedBy, string tokenHash, DateTime expiresAt, DateTime utcNow) : base(id)
    {
        OrganizationId = organizationId;
        InvitedUserEmail = invitedUserEmail;
        InvitedUserRole = invitedUserRole;
        InvitedUserPermissions = MemberRolePermissions.For(invitedUserRole);
        InvitedBy = invitedBy;
        TokenHash = tokenHash;
        Status = enInvitationStatus.Pending;
        ExpiresAt = expiresAt;
        CreatedAt = utcNow;
    }

    private OrganizationInvitation() { } // EF

    public Guid OrganizationId { get; private set; }
    public string InvitedUserEmail { get; private set; } = null!;
    public MemberRole InvitedUserRole { get; private set; }
    public enPermissions InvitedUserPermissions { get; private set; }
    public Guid InvitedBy { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public enInvitationStatus Status { get; private set; }

    /// <summary>When the invitation was accepted.</summary>
    public DateTime? RegisteredAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;

    /// <summary>An invitation can be accepted or declined only while it is pending and has not reached its expiration.</summary>
    public bool IsActionable(DateTime utcNow) => Status == enInvitationStatus.Pending && !IsExpired(utcNow);

    public static Result<OrganizationInvitation> Create(
        Guid organizationId, string email, MemberRole role,
        Guid invitedBy, string tokenHash, DateTime expiresAt, DateTime utcNow)
    {
        if (organizationId == Guid.Empty || invitedBy == Guid.Empty)
            return Result.Failure<OrganizationInvitation>(OrganizationInvitationErrors.InvalidReference);

        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<OrganizationInvitation>(OrganizationInvitationErrors.EmailRequired);

        if (string.IsNullOrWhiteSpace(tokenHash))
            return Result.Failure<OrganizationInvitation>(OrganizationInvitationErrors.TokenHashRequired);

        if (role == MemberRole.OrgOwner)
            return Result.Failure<OrganizationInvitation>(OrganizationInvitationErrors.CannotInviteAsOwner);

        if (expiresAt <= utcNow)
            return Result.Failure<OrganizationInvitation>(OrganizationInvitationErrors.InvalidExpiration);

        return new OrganizationInvitation(Guid.NewGuid(), organizationId, NormalizeEmail(email), role, invitedBy, tokenHash,
            expiresAt, utcNow);
    }

    /// <summary>Emails are stored trimmed and in lower case, so the same address always matches the same invitation.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public Result Accept(DateTime utcNow)
    {
        if (!IsActionable(utcNow)) return Result.Failure(OrganizationInvitationErrors.InvalidOrExpired);

        Status = enInvitationStatus.Accepted;
        RegisteredAt = utcNow;
        return Result.Success();
    }

    /// <summary>The invitee declines the invitation. It ends up cancelled, like an invitation withdrawn by the inviter.</summary>
    public Result Reject(DateTime utcNow)
    {
        if (!IsActionable(utcNow)) return Result.Failure(OrganizationInvitationErrors.InvalidOrExpired);

        Status = enInvitationStatus.Cancelled;
        return Result.Success();
    }

    /// <summary>The inviter or an admin withdraws a pending invitation. Accepted and expired invitations cannot be cancelled.</summary>
    public Result Cancel(DateTime utcNow)
    {
        if (Status == enInvitationStatus.Accepted) return Result.Failure(OrganizationInvitationErrors.AlreadyAccepted);
        if (Status == enInvitationStatus.Cancelled) return Result.Failure(OrganizationInvitationErrors.AlreadyCancelled);
        if (Status == enInvitationStatus.Expired || IsExpired(utcNow))
            return Result.Failure(OrganizationInvitationErrors.AlreadyExpired);

        Status = enInvitationStatus.Cancelled;
        return Result.Success();
    }

    /// <summary>Records that a pending invitation reached its expiration without being accepted.</summary>
    public Result MarkExpired(DateTime utcNow)
    {
        if (Status != enInvitationStatus.Pending || !IsExpired(utcNow))
            return Result.Failure(OrganizationInvitationErrors.InvalidState);

        Status = enInvitationStatus.Expired;
        return Result.Success();
    }

    /// <summary>Issues a new token and expiration for a pending or expired invitation, making it pending again.</summary>
    public Result Regenerate(string newTokenHash, DateTime newExpiresAt, DateTime utcNow)
    {
        if (Status is not (enInvitationStatus.Pending or enInvitationStatus.Expired))
            return Result.Failure(OrganizationInvitationErrors.InvalidState);

        if (string.IsNullOrWhiteSpace(newTokenHash))
            return Result.Failure(OrganizationInvitationErrors.TokenHashRequired);

        if (newExpiresAt <= utcNow)
            return Result.Failure(OrganizationInvitationErrors.InvalidExpiration);

        TokenHash = newTokenHash;
        ExpiresAt = newExpiresAt;
        Status = enInvitationStatus.Pending;
        return Result.Success();
    }
}
