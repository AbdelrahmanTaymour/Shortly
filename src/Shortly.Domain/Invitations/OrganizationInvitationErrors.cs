using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Invitations;

public static class OrganizationInvitationErrors
{
    public static readonly Error NotFound = Error.NotFound("OrganizationInvitation.NotFound", "Invitation not found.");
    public static readonly Error InvalidOrExpired = Error.Validation("OrganizationInvitation.InvalidOrExpired", "This invitation is invalid or has expired.");
    public static readonly Error InvalidState = Error.Validation("OrganizationInvitation.InvalidState", "Invitation is not in a state that allows this action.");
    public static readonly Error EmailRequired = Error.Validation("OrganizationInvitation.EmailRequired", "An email address is required.");
    public static readonly Error TokenHashRequired = Error.Validation("OrganizationInvitation.TokenHashRequired", "An invitation token is required.");
    public static readonly Error InvalidExpiration = Error.Validation("OrganizationInvitation.InvalidExpiration", "The invitation expiration must be in the future.");
    public static readonly Error InvalidReference = Error.Validation("OrganizationInvitation.InvalidReference", "The organization and the inviter are required.");
    public static readonly Error AlreadyAccepted = Error.Conflict("OrganizationInvitation.AlreadyAccepted", "This invitation has already been accepted.");
    public static readonly Error AlreadyExpired = Error.Conflict("OrganizationInvitation.AlreadyExpired", "This invitation has expired.");
    public static readonly Error AlreadyCancelled = Error.Conflict("OrganizationInvitation.AlreadyCancelled", "This invitation has already been cancelled.");
    public static readonly Error AlreadyPending = Error.Conflict("OrganizationInvitation.AlreadyPending", "This user already has a pending invitation to this organization.");
    public static readonly Error AlreadyMember = Error.Conflict("OrganizationInvitation.AlreadyMember", "This user is already a member of this organization.");
    public static readonly Error MemberLimitReached = Error.Forbidden("OrganizationInvitation.MemberLimitReached", "Organization has reached its member limit.");
    public static readonly Error NotInviter = Error.Forbidden("OrganizationInvitation.NotInviter", "Only the inviter or an organization admin can perform this action.");
    public static readonly Error EmailMismatch = Error.Forbidden("OrganizationInvitation.EmailMismatch", "The invited email does not match the current user.");
    public static readonly Error InsufficientRole = Error.Forbidden("OrganizationInvitation.InsufficientRole", "You don't have sufficient permissions to invite members to this organization.");
    public static readonly Error CannotInviteAsOwner = Error.Validation("OrganizationInvitation.CannotInviteAsOwner", "Cannot grant owner role directly.");
}