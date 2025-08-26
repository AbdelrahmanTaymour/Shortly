using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shortly.Core.Models;
using Shortly.Core.RepositoryContract.EmailService;

namespace Shortly.Infrastructure.Services;

/// <summary>
///     Provides email delivery via SMTP using <see cref="SmtpClient" />.
/// </summary>
/// <remarks>
///     This provider reads SMTP and general email configuration from <see cref="EmailSettings" />
///     and applies rules such as enabling/disabling email sending, blocking domains,
///     and batching for bulk operations.
/// </remarks>
public class SmtpEmailProvider : IEmailProvider
{
    private readonly EmailSettings _emailSettings;
    private readonly GeneralSettings _generalSettings;
    private readonly ILogger<SmtpEmailProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmtpEmailProvider"/> class.
    /// </summary>
    /// <param name="emailSettings">The configured email settings, including SMTP and general rules.</param>
    /// <param name="logger">The logger used for diagnostic and error logging.</param>
    public SmtpEmailProvider(
        IOptions<EmailSettings> emailSettings,
        ILogger<SmtpEmailProvider> logger)
    {
        _emailSettings = emailSettings.Value;
        _generalSettings = _emailSettings.General;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(EmailRequest request)
    {
        try
        {
            // Check if email sending is enabled
            if (!_generalSettings.EnableEmailSending)
            {
                _logger.LogWarning("Email sending is disabled in configuration");
                return EmailResult.Failure("Email sending is disabled");
            }

            // Validate recipient email
            if (!IsEmailAllowed(request.To))
            {
                _logger.LogWarning("Email sending blocked for recipient: {To}", request.To);
                return EmailResult.Failure($"Email sending not allowed for: {request.To}");
            }

            await SendEmailInternal(request);
            _logger.LogInformation("Email sent successfully to {To}", request.To);
            return EmailResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP email sending failed for {To}", request.To);
            return EmailResult.Failure("SMTP sending failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<List<EmailResult>> SendBulkAsync(List<EmailRequest> requests)
    {
        var results = new List<EmailResult>();
        var batchSize = _generalSettings.BulkEmailBatchSize;
        var delayBetweenBatches = _generalSettings.BulkEmailDelayBetweenBatches;

        _logger.LogInformation("Starting bulk email send for {Count} emails in batches of {BatchSize}",
            requests.Count, batchSize);

        for (var i = 0; i < requests.Count; i += batchSize)
        {
            var batch = requests.Skip(i).Take(batchSize).ToList();
            _logger.LogInformation("Processing batch {BatchNumber}/{TotalBatches}",
                i / batchSize + 1, (int)Math.Ceiling((double)requests.Count / batchSize));

            var batchTasks = batch.Select(SendAsync);
            var batchResults = await Task.WhenAll(batchTasks);
            results.AddRange(batchResults);

            // Delay between batches to avoid overwhelming the SMTP server
            if (i + batchSize < requests.Count && delayBetweenBatches > 0) await Task.Delay(delayBetweenBatches);
        }

        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => !r.IsSuccess);

        _logger.LogInformation("Bulk email completed: {SuccessCount} succeeded, {FailureCount} failed",
            successCount, failureCount);

        return results;
    }

    /// <summary>
    /// Handles the actual SMTP message construction and sending logic.
    /// </summary>
    /// <param name="request">The <see cref="EmailRequest"/> to send.</param>
    private async Task SendEmailInternal(EmailRequest request)
    {
        var smtp = _emailSettings.Smtp;

        using var client = new SmtpClient(smtp.Host, smtp.Port);
        client.EnableSsl = smtp.EnableSsl;
        client.UseDefaultCredentials = smtp.UseDefaultCredentials;
        client.Timeout = smtp.Timeout;

        if (!smtp.UseDefaultCredentials)
            client.Credentials = new NetworkCredential(smtp.Username, smtp.Password, smtp.Domain);

        using var message = new MailMessage();
        message.From = new MailAddress(smtp.FromEmail, smtp.FromName);
        message.Subject = request.Subject;
        message.Body = request.Body;
        message.IsBodyHtml = request.IsHtml;

        message.To.Add(request.To);

        // Add CC recipients
        foreach (var cc in (request.Cc ?? Enumerable.Empty<string>())
                 .Where(email => !string.IsNullOrWhiteSpace(email) && IsEmailAllowed(email)))
        {
            message.CC.Add(cc);
        }

        // Add BCC recipients
        foreach (var bcc in (request.Bcc ?? Enumerable.Empty<string>())
                 .Where(email => !string.IsNullOrWhiteSpace(email) && IsEmailAllowed(email)))
        {
            message.Bcc.Add(bcc);
        }

        if (_generalSettings.LogEmailContent)
            _logger.LogDebug("Sending email - To: {To}, Subject: {Subject}, Body Length: {BodyLength}",
                request.To, request.Subject, request.Body.Length);

        await client.SendMailAsync(message);
    }

   
    /// <summary>
    /// Validates whether a given email address is allowed to receive emails 
    /// based on configured allowed and blocked domains.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <returns><c>true</c> if the email is permitted; otherwise, <c>false</c>.</returns>
    private bool IsEmailAllowed(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var domain = email.Split('@').LastOrDefault()?.ToLower();
        if (string.IsNullOrWhiteSpace(domain))
            return false;

        // Check blocked domains
        if (_generalSettings.BlockedDomains.Count != 0)
            if (_generalSettings.BlockedDomains.Any(blocked =>
                    domain.Equals(blocked, StringComparison.OrdinalIgnoreCase)))
                return false;

        // Check allowed domains (if specified)
        if (_generalSettings.AllowedDomains.Count != 0)
            return _generalSettings.AllowedDomains.Any(allowed =>
                domain.Equals(allowed, StringComparison.OrdinalIgnoreCase));

        return true;
    }
}