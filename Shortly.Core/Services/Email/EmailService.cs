using Microsoft.Extensions.Logging;
using Shortly.Core.Models;
using Shortly.Core.RepositoryContract.EmailService;
using Shortly.Core.ServiceContracts.Email;

namespace Shortly.Core.Services.Email;

/// <inheritdoc />
/// <remarks>
///     This service acts as a wrapper around the email provider implementation, handling logging,
///     input validation, and error handling for both single and bulk email operations.
/// </remarks>
public class EmailService(IEmailProvider emailProvider, ILogger<EmailService> logger) : IEmailService
{
    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(EmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.To))
            return EmailResult.Failure("Email recipient is required");

        if (string.IsNullOrWhiteSpace(request.Subject))
            return EmailResult.Failure("Email subject is required");

        try
        {
            logger.LogInformation("Sending email to {To} with subject: {Subject}", request.To, request.Subject);
            var result = await emailProvider.SendAsync(request);

            if (result.IsSuccess)
                logger.LogInformation("Email sent successfully to {To}", request.To);
            else
                logger.LogWarning("Failed to send email to {To}: {Message}", request.To, result.Message);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email to {To}", request.To);
            return EmailResult.Failure("An error occurred while sending email", ex);
        }
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendBulkAsync(List<EmailRequest> requests)
    {
        if (requests.Count == 0)
            return EmailResult.Failure("No email requests provided");

        try
        {
            logger.LogInformation("Sending bulk emails to {Count} recipients", requests.Count);
            var results = await emailProvider.SendBulkAsync(requests);

            var failedCount = results.Count(r => !r.IsSuccess);
            var successCount = results.Count(r => r.IsSuccess);

            if (failedCount == 0)
            {
                logger.LogInformation("All {Count} bulk emails sent successfully", successCount);
                return EmailResult.Success();
            }

            if (successCount == 0)
            {
                logger.LogWarning("All {Count} bulk emails failed to send", failedCount);
                return EmailResult.Failure($"All {failedCount} emails failed to send");
            }

            logger.LogWarning("Bulk email partial success: {SuccessCount} succeeded, {FailedCount} failed",
                successCount, failedCount);
            return EmailResult.Failure($"Partial success: {successCount} succeeded, {failedCount} failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending bulk emails");
            return EmailResult.Failure("An error occurred while sending bulk emails", ex);
        }
    }
}