namespace Shortly.Domain.Invitations;

/// <summary>
/// Pending: the invitation can still be accepted.
/// Accepted: the invitee accepted it and became a member.
/// Expired: it was not accepted before its expiration date.
/// Cancelled: it was withdrawn by the inviter or declined by the invitee.
/// Invitations are never deleted; the status keeps the history.
/// </summary>
public enum enInvitationStatus
{
    Pending = 1,
    Accepted = 2,
    Expired = 3,
    Cancelled = 4
}
