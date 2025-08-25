using Shortly.Core.Models;

namespace Shortly.Core.ServiceContracts.Email;

/// <summary>
/// Service for generating predefined and custom email templates used in the Shortly application.
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Generates an email verification template for confirming a new user's email address.
    /// </summary>
    /// <param name="userName">The recipient's username or display name.</param>
    /// <param name="verificationLink">The verification link the user must click.</param>
    /// <returns>An <see cref="EmailTemplate"/> containing the subject, HTML body, and formatting.</returns>
    EmailTemplate GetEmailVerificationTemplateAsync(string userName, string verificationLink);
   
    /// <summary>
    /// Generates a password reset email template for users who request to reset their password.
    /// </summary>
    /// <param name="userName">The recipient's username or display name.</param>
    /// <param name="resetLink">The password reset link provided to the user.</param>
    /// <returns>An <see cref="EmailTemplate"/> with subject, HTML body, and expiration information.</returns>
    EmailTemplate GetPasswordResetTemplateAsync(string userName, string resetLink);
    
    /// <summary>
    /// Generates an invitation email template for inviting a user to join an organization.
    /// </summary>
    /// <param name="inviterName">The name of the person sending the invitation.</param>
    /// <param name="inviteeName">The name of the invitee.</param>
    /// <param name="invitationLink">The link the invitee can use to accept the invitation.</param>
    /// <param name="organizationName">The name of the organization.</param>
    /// <returns>An <see cref="EmailTemplate"/> containing the invitation details.</returns>
    EmailTemplate GetUserInvitationTemplateAsync(string inviterName, string inviteeName, string invitationLink, string organizationName);
  
    /// <summary>
    /// Generates a custom email template based on provided parameters.
    /// </summary>
    /// <param name="templateName">The name of the custom template for logging/reference.</param>
    /// <param name="parameters">
    /// A dictionary of parameters including:
    /// <list type="bullet">
    /// <item><c>subject</c>: Email subject (default: "Notification")</item>
    /// <item><c>body</c>: Email body content (default: "This is a custom email template.")</item>
    /// <item><c>isHtml</c>: Indicates if the body is HTML (default: "true")</item>
    /// </list>
    /// </param>
    /// <returns>An <see cref="EmailTemplate"/> built from the provided parameters.</returns>
    EmailTemplate GetCustomTemplateAsync(string templateName, Dictionary<string, string> parameters);
}