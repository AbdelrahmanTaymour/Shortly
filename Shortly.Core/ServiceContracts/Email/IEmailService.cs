using Shortly.Core.Models;
using Shortly.Core.RepositoryContract.EmailService;

namespace Shortly.Core.ServiceContracts.Email;

/// <summary>
/// Provides functionality to send individual and bulk emails using the configured <see cref="IEmailProvider"/>.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a single email message to a specified recipient.
    /// </summary>
    /// <param name="request">The <see cref="EmailRequest"/> containing recipient, subject, body, and formatting details.</param>
    /// <returns>
    /// An <see cref="EmailResult"/> indicating whether the email was successfully sent.  
    /// Includes error details if validation fails or the provider reports a failure.
    /// </returns>
    /// <remarks>
    /// - Validates that the recipient (<c>To</c>) and subject are not null or empty.  
    /// - Logs the operation and the outcome (success, failure, or error).  
    /// - Catches and logs exceptions, returning a failed result in such cases.  
    /// </remarks>
    Task<EmailResult> SendAsync(EmailRequest request);
    
    /// <summary>
    /// Sends multiple emails in a single bulk operation.
    /// </summary>
    /// <param name="requests">A list of <see cref="EmailRequest"/> objects representing the emails to be sent.</param>
    /// <returns>
    /// An <see cref="EmailResult"/> summarizing the outcome:
    /// <list type="bullet">
    ///   <item><description><b>Success</b> if all emails are sent successfully.</description></item>
    ///   <item><description><b>Failure</b> if all emails fail.</description></item>
    ///   <item><description><b>Partial success</b> if some emails succeed and others fail.</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// - Validates that the request list is not empty.  
    /// - Logs the number of emails being sent and their results.  
    /// - Distinguishes between full success, full failure, and partial success scenarios.  
    /// - Catches and logs exceptions, returning a failure result with error details.  
    /// </remarks>
    Task<EmailResult> SendBulkAsync(List<EmailRequest> requests);
}