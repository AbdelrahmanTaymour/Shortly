using Shortly.Domain.Abstractions;
using Shortly.Domain.Enums;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Domain.Invitations;

public sealed class OrganizationInvitation : Entity<Guid>
{
    private OrganizationInvitation(
        Guid id, Guid organizationId, string invitedUserEmail, MemberRole invitedUserRole,
        enPermissions invitedUserPermissions, Guid invitedBy, string tokenHash, DateTime expiresAt, DateTime utcNow) : base(id)
    {
        OrganizationId = organizationId;
        InvitedUserEmail = invitedUserEmail;
        InvitedUserRole = invitedUserRole;
        InvitedUserPermissions = invitedUserPermissions;
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
    public DateTime? RegisteredAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsExpired(DateTime utcNow) => ExpiresAt < utcNow;

    public bool IsActionable(DateTime utcNow) =>
        Status is enInvitationStatus.Pending or enInvitationStatus.EmailSent && !IsExpired(utcNow);

    public static OrganizationInvitation Create(
        Guid organizationId, string email, MemberRole role, enPermissions permissions,
        Guid invitedBy, string tokenHash, DateTime expiresAt, DateTime utcNow)
        => new(Guid.NewGuid(), organizationId, email, role, permissions, invitedBy, tokenHash, expiresAt, utcNow);


    public Result MarkAsSent()
    {
        if (Status != enInvitationStatus.Pending) return Result.Failure(OrganizationInvitationErrors.InvalidState);
        Status = enInvitationStatus.EmailSent;
        return Result.Success();
    }

    public Result Accept(DateTime utcNow)
    {
        if (!IsActionable(utcNow)) return Result.Failure(OrganizationInvitationErrors.InvalidOrExpired);
        Status = enInvitationStatus.Registered;
        RegisteredAt = utcNow;
        return Result.Success();
    }

    public Result Reject(DateTime utcNow)
    {
        if (!IsActionable(utcNow)) return Result.Failure(OrganizationInvitationErrors.InvalidOrExpired);
        Status = enInvitationStatus.Rejected;
        return Result.Success();
    }

    
    public Result Regenerate(string newTokenHash, DateTime newExpiresAt)
    {
        if (Status != enInvitationStatus.EmailSent) return Result.Failure(OrganizationInvitationErrors.InvalidState);
        TokenHash = newTokenHash;
        ExpiresAt = newExpiresAt;
        return Result.Success();
    }
}