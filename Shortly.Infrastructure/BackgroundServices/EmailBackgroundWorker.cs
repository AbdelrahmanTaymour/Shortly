using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shortly.Core.Models;
using Shortly.Core.RepositoryContract.EmailService;

namespace Shortly.Infrastructure.BackgroundServices;

public class EmailBackgroundWorker(
    EmailQueueService emailQueueService, 
    IServiceProvider serviceProvider,
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
                // Dequeue the next email request
                var emailRequest = await emailQueueService.DequeueEmailAsync(stoppingToken);
                if (emailRequest != null)
                {
                    logger.LogInformation("Processing email for {Email}.", emailRequest.To);

                    // Use IServiceProvider to create a new scope
                    using var scope = serviceProvider.CreateScope();
                    var emailProvider = scope.ServiceProvider.GetRequiredService<IEmailProvider>();

                    // Send the email with the scoped IEmailProvider
                    var result = await emailProvider.SendAsync(emailRequest);

                    if (result.IsSuccess)
                    {
                        logger.LogInformation("Email sent successfully to {Email}.", emailRequest.To);

                        // Add your logic for updating the status (e.g., EmailSent)
                    }
                    else
                    {
                        logger.LogError("Failed to send email to {Email}. Error: {Error}", emailRequest.To, result.Message);
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