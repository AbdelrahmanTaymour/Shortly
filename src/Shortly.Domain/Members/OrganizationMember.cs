using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Members;

public sealed class OrganizationMember : Entity<Guid>
{
    private OrganizationMember(Guid id, Guid organizationId, Guid userId, MemberRole role, Guid invitedBy,
        DateTime utcNow) : base(id)
    {
        OrganizationId = organizationId;
        UserId = userId;
        Role = role;
        IsActive = true;
        InvitedBy = invitedBy;
        JoinedAt = utcNow;
    }

    private OrganizationMember()
    {
    } // EF

    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public MemberRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public Guid InvitedBy { get; private set; }
    public DateTime JoinedAt { get; private set; }

    public static OrganizationMember Invite(Guid organizationId, Guid userId, MemberRole role, Guid invitedBy,
        DateTime utcNow)
    {
        return new OrganizationMember(Guid.NewGuid(), organizationId, userId, role, invitedBy, utcNow);
    }

    public static OrganizationMember CreateOwner(Guid organizationId, Guid userId, DateTime utcNow)
    {
        return new OrganizationMember(Guid.NewGuid(), organizationId, userId, MemberRole.OrgOwner, userId, utcNow);
    }

    public Result ChangeRole(MemberRole newRole)
    {
        if (Role == MemberRole.OrgOwner) return Result.Failure(OrganizationMemberErrors.CannotChangeOwnerRole);
        if (newRole == MemberRole.OrgOwner)
            return Result.Failure(OrganizationMemberErrors.CannotGrantOwnerRoleDirectly);
        Role = newRole;
        return Result.Success();
    }
    
    public void PromoteToOwner() => Role = MemberRole.OrgOwner;

    public Result DemoteFromOwner(MemberRole fallbackRole)
    {
        if (Role != MemberRole.OrgOwner) return Result.Failure(OrganizationMemberErrors.NotOwner);
        if (fallbackRole == MemberRole.OrgOwner) return Result.Failure(OrganizationMemberErrors.CannotGrantOwnerRoleDirectly);
        Role = fallbackRole;
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (Role == MemberRole.OrgOwner) return Result.Failure(OrganizationMemberErrors.CannotRemoveOwner);
        if (!IsActive) return Result.Failure(OrganizationMemberErrors.AlreadyInactive);
        IsActive = false;
        return Result.Success();
    }

    public Result Reactivate()
    {
        if (IsActive) return Result.Failure(OrganizationMemberErrors.AlreadyActive);
        IsActive = true;
        return Result.Success();
    }
}