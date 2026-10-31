using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shortly.Infrastructure.Email;

internal sealed class EmailBackgroundWorker(
    EmailQueueService emailQueueService, 
    IEmailSender emailSender,
    ILogger<EmailBackgroundWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email background worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var emailRequest = await emailQueueService.DequeueEmailAsync(stoppingToken);
                if (emailRequest != null)
                {
                    logger.LogInformation("Processing email for {Email}.", emailRequest.To);

                    var result = await emailSender.SendAsync(emailRequest, stoppingToken);

                    if (result.IsSuccess)
                    {
                        logger.LogInformation("Email sent successfully to {Email}.", emailRequest.To);
                    }
                    else
                    {
                        logger.LogError("Failed to send email to {Email}. Error: {Error}", emailRequest.To, result.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred in the email background worker.");
            }
        }
    }
}