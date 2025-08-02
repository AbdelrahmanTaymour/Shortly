namespace Shortly.Domain.Enums;

/// <summary>
/// Pending: The system has queued the invitation email for sending.
/// Email Sent: The system has successfully delivered the invitation email.
/// Failure: The email delivery failed because the email address is incorrect or not in use.
/// User Clicked: The user clicked the registration link in the email message but did not register.
/// Registered: The user successfully registered, and the system created a user profile.
/// </summary>
public enum enInvitationStatus
{
    Pending = 1,
    EmailSent,
    Failure,
    UserClicked,
    Registered,
}

