using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shortly.Domain.Abstractions;

namespace Shortly.Infrastructure.Email;

internal sealed class SmtpEmailSender(
    IOptions<EmailOptions> emailOptions, 
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = emailOptions.Value;

    public async Task<Result> SendAsync(EmailRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_options.Smtp.FromEmail, _options.Smtp.FromName);
            message.Subject = request.Subject;
            message.Body = request.Body;
            message.IsBodyHtml = request.IsHtml;
            message.To.Add(request.To);

            using var client = new SmtpClient(_options.Smtp.Host, _options.Smtp.Port);
            client.EnableSsl = _options.Smtp.EnableSsl;
            client.UseDefaultCredentials = _options.Smtp.UseDefaultCredentials;
            client.Credentials = new NetworkCredential(_options.Smtp.Username, _options.Smtp.Password);
            client.Timeout = _options.Smtp.Timeout;

            await client.SendMailAsync(message, cancellationToken);

            logger.LogInformation("Email successfully sent to {Email} with subject {Subject}", request.To, request.Subject);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email} with subject {Subject}", request.To, request.Subject);
            return Result.Failure(new Error("Email.SendFailed", ex.Message));
        }
    }
}