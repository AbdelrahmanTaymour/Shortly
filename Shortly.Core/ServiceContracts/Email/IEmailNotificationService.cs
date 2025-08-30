namespace Shortly.Core.ServiceContracts.Email;

/// <summary>
/// Service responsible for sending different types of notification emails such as
/// email verification, password resets, and user invitations.
/// </summary>
public interface IEmailNotificationService
{
    /// <summary>
    /// Sends an email verification message to a user.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="userName">The recipient's display name or username.</param>
    /// <param name="verificationToken">The unique verification token associated with the user.</param>
    void EnqueueSendEmailVerificationAsync(string email, string userName, string verificationToken);
   
    /// <summary>
    /// Sends a password-reset email to a user who requested a password change.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="userName">The recipient's display name or username.</param>
    /// <param name="resetToken">The unique reset token generated for the user.</param>
    void EnqueueSendPasswordResetAsync(string email, string userName, string resetToken);
  
    /// <summary>
    /// Sends an invitation email to a new user to join an organization.
    /// </summary>
    /// <param name="email">The invitee's email address.</param>
    /// <param name="inviterUsername">The name of the person sending the invitation.</param>
    /// <param name="inviteeName">The username of the person being invited.</param>
    /// <param name="invitationToken">The unique token for accepting the invitation.</param>
    /// <param name="organizationName">The name of the organization the invitee is asked to join.</param>
    void EnqueueSendUserInvitation(string email, string inviterUsername, string inviteeName, string invitationToken, string organizationName);
}