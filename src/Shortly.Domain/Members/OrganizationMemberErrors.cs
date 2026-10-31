using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Members;

public static class OrganizationMemberErrors
{
    public static readonly Error NotFound = Error.NotFound("OrganizationMember.NotFound", "Membership not found.");
    public static readonly Error AlreadyMember = Error.Conflict("OrganizationMember.AlreadyMember", "User is already a member of this organization.");
    public static readonly Error MemberLimitReached = Error.Forbidden("OrganizationMember.MemberLimitReached", "Organization has reached its member limit.");
    public static readonly Error NotOwner = Error.Validation("OrganizationMember.NotOwner", "Member does not currently hold the owner role.");public static readonly Error CannotRemoveOwner = Error.Validation("OrganizationMember.CannotRemoveOwner", "The organization owner cannot be removed.");
    public static readonly Error CannotChangeOwnerRole = Error.Validation("OrganizationMember.CannotChangeOwnerRole", "The organization owner's role cannot be changed directly.");
    public static readonly Error CannotGrantOwnerRoleDirectly = Error.Validation("OrganizationMember.CannotGrantOwnerRoleDirectly", "Ownership can only change via organization transfer.");
    public static readonly Error InsufficientRole = Error.Forbidden("OrganizationMember.InsufficientRole", "You don't have sufficient permissions within this organization.");
    public static readonly Error AlreadyActive = Error.Conflict("OrganizationMember.AlreadyActive", "Member is already active.");
    public static readonly Error AlreadyInactive = Error.Conflict("OrganizationMember.AlreadyInactive", "Member is already inactive.");
}