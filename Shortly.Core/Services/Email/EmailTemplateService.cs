using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shortly.Core.Models;
using Shortly.Core.ServiceContracts.Email;

namespace Shortly.Core.Services.Email;

/// <inheritdoc />
/// <remarks>
///     This service provides ready-to-use templates for email verification, password resets,
///     user invitations, and custom email messages. Templates are styled HTML emails and can be
///     configured via <see cref="AppSettings" />.
/// </remarks>
public class EmailTemplateService(
    IOptions<AppSettings> appSettings,
    ILogger<EmailTemplateService> logger)
    : IEmailTemplateService
{
    private readonly AppSettings _appSettings = appSettings.Value;

    /// <inheritdoc />
    public EmailTemplate GetEmailVerificationTemplateAsync(string userName, string verificationLink)
    {
        var expiryHours = _appSettings.Tokens.EmailVerificationExpiryHours;

        return new EmailTemplate
        {
            Subject = $"Verify your email address - {_appSettings.Name}",
            Body = GetEmailVerificationTemplate(userName, verificationLink, expiryHours),
            IsHtml = true
        };
    }

    /// <inheritdoc />
    public EmailTemplate GetPasswordResetTemplateAsync(string userName, string resetLink)
    {
        var expiryHours = _appSettings.Tokens.PasswordResetExpiryHours;

        return new EmailTemplate
        {
            Subject = $"Reset your password - {_appSettings.Name}",
            Body = GetPasswordResetTemplate(userName, resetLink, expiryHours),
            IsHtml = true
        };
    }

    /// <inheritdoc />
    public EmailTemplate GetUserInvitationTemplateAsync(string inviterName, string inviteeName,
        string invitationLink, string organizationName)
    {
        var expiryDays = _appSettings.Tokens.InvitationExpiryDays;

        return new EmailTemplate
        {
            Subject = $"You're invited to join {organizationName} on {_appSettings.Name}",
            Body = GetUserInvitationTemplate(inviterName, inviteeName, invitationLink, organizationName,
                expiryDays),
            IsHtml = true
        };
    }

    /// <inheritdoc />
    public EmailTemplate GetCustomTemplateAsync(string templateName,
        Dictionary<string, string> parameters)
    {
        logger.LogInformation("Loading custom template: {TemplateName}", templateName);

        return new EmailTemplate
        {
            Subject = parameters.GetValueOrDefault("subject", "Notification"),
            Body = parameters.GetValueOrDefault("body", "This is a custom email template."),
            IsHtml = bool.Parse(parameters.GetValueOrDefault("isHtml", "true"))
        };
    }

    /// <summary>
    ///     Builds the HTML body for an email verification message.
    /// </summary>
    private string GetEmailVerificationTemplate(string userName, string verificationLink, int expiryHours)
    {
        return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Email Verification</title>
                </head>
                <body style='margin: 0; padding: 20px; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        {GetEmailHeader()}
                        <div style='padding: 30px;'>
                            <h2 style='color: #333; margin-top: 0; text-align: center;'>Verify Your Email Address</h2>
                            <p style='color: #666; line-height: 1.6;'>Hello {userName},</p>
                            <p style='color: #666; line-height: 1.6;'>
                                Welcome to {_appSettings.Name}! To complete your registration and secure your account, 
                                please verify your email address by clicking the button below.
                            </p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{verificationLink}' 
                                   style='background-color: #007bff; color: white; padding: 14px 28px; text-decoration: none; 
                                          border-radius: 6px; display: inline-block; font-weight: bold; font-size: 16px;'>
                                    Verify Email Address
                                </a>
                            </div>
                            <p style='color: #666; line-height: 1.6;'>
                                If the button above doesn't work, you can copy and paste the following link into your browser:
                            </p>
                            <p style='background-color: #f8f9fa; padding: 15px; border-radius: 4px; word-break: break-all; 
                                      color: #495057; border-left: 4px solid #007bff; margin: 20px 0;'>
                                {verificationLink}
                            </p>
                            <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; border-radius: 4px; padding: 15px; margin: 20px 0;'>
                                <p style='margin: 0; color: #856404; font-size: 14px;'>
                                    <strong>Important:</strong> This verification link will expire in {expiryHours} hours for security reasons.
                                </p>
                            </div>
                        </div>
                        {GetEmailFooter()}
                    </div>
                </body>
                </html>";
    }

    /// <summary>
    ///     Builds the HTML body for a password reset message.
    /// </summary>
    private string GetPasswordResetTemplate(string userName, string resetLink, int expiryHours)
    {
        return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Password Reset</title>
                </head>
                <body style='margin: 0; padding: 20px; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        {GetEmailHeader()}
                        <div style='padding: 30px;'>
                            <h2 style='color: #333; margin-top: 0; text-align: center;'>Reset Your Password</h2>
                            <p style='color: #666; line-height: 1.6;'>Hello {userName},</p>
                            <p style='color: #666; line-height: 1.6;'>
                                We received a request to reset the password for your {_appSettings.Name} account. 
                                If you made this request, click the button below to reset your password.
                            </p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetLink}' 
                                   style='background-color: #dc3545; color: white; padding: 14px 28px; text-decoration: none; 
                                          border-radius: 6px; display: inline-block; font-weight: bold; font-size: 16px;'>
                                    Reset Password
                                </a>
                            </div>
                            <p style='color: #666; line-height: 1.6;'>
                                If the button above doesn't work, you can copy and paste the following link into your browser:
                            </p>
                            <p style='background-color: #f8f9fa; padding: 15px; border-radius: 4px; word-break: break-all; 
                                      color: #495057; border-left: 4px solid #dc3545; margin: 20px 0;'>
                                {resetLink}
                            </p>
                            <div style='background-color: #f8d7da; border: 1px solid #f5c6cb; border-radius: 4px; padding: 15px; margin: 20px 0;'>
                                <p style='margin: 0; color: #721c24; font-size: 14px;'>
                                    <strong>Security Notice:</strong> This password reset link will expire in {expiryHours} hour(s). 
                                    If you didn't request this reset, please ignore this email.
                                </p>
                            </div>
                        </div>
                        {GetEmailFooter()}
                    </div>
                </body>
                </html>";
    }

    /// <summary>
    ///     Builds the HTML body for a user invitation message.
    /// </summary>
    private string GetUserInvitationTemplate(string inviterName, string inviteeName, string invitationLink,
        string organizationName, int expiryDays)
    {
        return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>You're Invited!</title>
                </head>
                <body style='margin: 0; padding: 20px; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        {GetEmailHeader()}
                        <div style='padding: 30px;'>
                            <h2 style='color: #333; margin-top: 0; text-align: center;'>You're Invited to Join {organizationName}!</h2>
                            <p style='color: #666; line-height: 1.6;'>Hello {inviteeName},</p>
                            <p style='color: #666; line-height: 1.6;'>
                                Great news! <strong>{inviterName}</strong> has invited you to join <strong>{organizationName}</strong> 
                                on {_appSettings.Name}. You'll have access to collaborate with the team and enjoy all the features.
                            </p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{invitationLink}' 
                                   style='background-color: #28a745; color: white; padding: 14px 28px; text-decoration: none; 
                                          border-radius: 6px; display: inline-block; font-weight: bold; font-size: 16px;'>
                                    Accept Invitation
                                </a>
                            </div>
                            <p style='color: #666; line-height: 1.6;'>
                                If the button above doesn't work, you can copy and paste the following link into your browser:
                            </p>
                            <p style='background-color: #f8f9fa; padding: 15px; border-radius: 4px; word-break: break-all; 
                                      color: #495057; border-left: 4px solid #28a745; margin: 20px 0;'>
                                {invitationLink}
                            </p>
                            <div style='background-color: #d1ecf1; border: 1px solid #bee5eb; border-radius: 4px; padding: 15px; margin: 20px 0;'>
                                <p style='margin: 0; color: #0c5460; font-size: 14px;'>
                                    <strong>Note:</strong> This invitation will expire in {expiryDays} days. 
                                    If you don't want to join, you can simply ignore this email.
                                </p>
                            </div>
                        </div>
                        {GetEmailFooter()}
                    </div>
                </body>
                </html>";
    }

    /// <summary>
    ///     Generates the standard email header section including logo and app name.
    /// </summary>
    private string GetEmailHeader()
    {
        var logoSection = !string.IsNullOrWhiteSpace(_appSettings.LogoUrl)
            ? $"<img src='{_appSettings.LogoUrl}' alt='{_appSettings.Name}' style='max-height: 60px; margin-bottom: 20px;'>"
            : "";

        return $@"
                <div style='background-color: #f8f9fa; padding: 20px; text-align: center; border-top-left-radius: 8px; border-top-right-radius: 8px;'>
                    {logoSection}
                    <h1 style='margin: 0; color: #333; font-size: 24px;'>{_appSettings.Name}</h1>
                </div>";
    }

    /// <summary>
    ///     Generates the standard email footer including copyright
    ///     and support contact information.
    /// </summary>
    private string GetEmailFooter()
    {
        var supportEmailSection = !string.IsNullOrWhiteSpace(_appSettings.SupportEmail)
            ? $"<p style='margin: 10px 0 0 0; color: #999; font-size: 12px;'>Need help? Contact us at <a href='mailto:{_appSettings.SupportEmail}' style='color: #007bff;'>{_appSettings.SupportEmail}</a></p>"
            : "";

        return $@"
                <div style='background-color: #f8f9fa; padding: 20px; text-align: center; border-bottom-left-radius: 8px; border-bottom-right-radius: 8px; border-top: 1px solid #dee2e6;'>
                    <p style='margin: 0; color: #999; font-size: 12px;'>
                        © {DateTime.Now.Year} {_appSettings.Name}. All rights reserved.
                    </p>
                    {supportEmailSection}
                </div>";
    }
}