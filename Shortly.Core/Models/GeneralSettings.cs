namespace Shortly.Core.Models;

public record GeneralSettings
{
    public bool EnableEmailSending { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 1000;
    public int BulkEmailBatchSize { get; set; } = 50;
    public int BulkEmailDelayBetweenBatches { get; set; } = 100;
    public bool LogEmailContent { get; set; } = false;
    public List<string> AllowedDomains { get; set; } = new List<string>();
    public List<string> BlockedDomains { get; set; } = new List<string>();
}