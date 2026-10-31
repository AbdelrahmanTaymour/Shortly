using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Invitations;

public static class OrganizationInvitationErrors
{
    public static readonly Error NotFound = Error.NotFound("OrganizationInvitation.NotFound", "Invitation not found.");
    public static readonly Error InvalidOrExpired = Error.Validation("OrganizationInvitation.InvalidOrExpired", "This invitation is invalid or has expired.");
    public static readonly Error InvalidState = Error.Validation("OrganizationInvitation.InvalidState", "Invitation is not in a state that allows this action.");
    public static readonly Error AlreadyPending = Error.Conflict("OrganizationInvitation.AlreadyPending", "This user already has a pending invitation to this organization.");
    public static readonly Error AlreadyMember = Error.Conflict("OrganizationInvitation.AlreadyMember", "This user is already a member of this organization.");
    public static readonly Error MemberLimitReached = Error.Forbidden("OrganizationInvitation.MemberLimitReached", "Organization has reached its member limit.");
    public static readonly Error NotInviter = Error.Forbidden("OrganizationInvitation.NotInviter", "Only the inviter or an organization admin can perform this action.");
    public static readonly Error EmailMismatch = Error.Forbidden("OrganizationInvitation.EmailMismatch", "The invited email does not match the current user.");
    public static readonly Error InsufficientRole = Error.Forbidden("OrganizationInvitation.InsufficientRole", "You don't have sufficient permissions to invite members to this organization.");
    public static readonly Error CannotInviteAsOwner = Error.Validation("OrganizationInvitation.CannotInviteAsOwner", "Cannot grant owner role directly.");
}