using System.Net.Mail;
using Shortly.Core.Models;

namespace Shortly.Core.RepositoryContract.EmailService;

/// <summary>
/// Defins email delivery via SMTP using <see cref="SmtpClient"/>.
/// </summary>
public interface IEmailProvider
{
    /// <summary>
    /// Sends a single email using SMTP.
    /// </summary>
    /// <param name="request">The <see cref="EmailRequest"/> containing recipient, subject, body, and metadata.</param>
    /// <returns>
    /// An <see cref="EmailResult"/> indicating success or failure of the operation.
    /// </returns>
    /// <remarks>
    /// - Skips sending if email sending is disabled in configuration.  
    /// - Validates recipient email against allowed/blocked domains.  
    /// - Logs email delivery results.  
    /// - Exceptions are caught and returned as failed results.  
    /// </remarks>
    Task<EmailResult> SendAsync(EmailRequest request);
    
    /// <summary>
    /// Sends multiple emails in batches via SMTP.
    /// </summary>
    /// <param name="requests">The list of <see cref="EmailRequest"/> messages to send.</param>
    /// <returns>
    /// A list of <see cref="EmailResult"/> objects, one for each email attempted.
    /// </returns>
    /// <remarks>
    /// - Splits requests into batches based on configured batch size.  
    /// - Introduces delays between batches to avoid overwhelming the SMTP server.  
    /// - Logs batch progress, successes, and failures.  
    /// </remarks>
    Task<List<EmailResult>> SendBulkAsync(List<EmailRequest> requests);
}