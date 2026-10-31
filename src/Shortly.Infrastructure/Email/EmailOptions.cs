namespace Shortly.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "EmailSettings";

    public string Provider { get; init; } = "smtp";
    
    public SmtpOptions Smtp { get; init; } = new();
    
    public EmailGeneralOptions General { get; init; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; init; } = string.Empty;
    
    public int Port { get; init; } = 587;
    
    public string Username { get; init; } = string.Empty;
    
    public string Password { get; init; } = string.Empty;
    
    public string FromEmail { get; init; } = string.Empty;
    
    public string FromName { get; init; } = string.Empty;
    
    public bool EnableSsl { get; init; } = true;
    
    public bool UseDefaultCredentials { get; init; } = false;
    
    public int Timeout { get; init; } = 30000;
    
    public string Domain { get; init; } = string.Empty;
}

public sealed class EmailGeneralOptions
{
    public bool EnableEmailSending { get; init; } = true;
    
    public int MaxRetryAttempts { get; init; } = 3;
    
    public int RetryDelayMilliseconds { get; init; } = 1000;
    
    public int BulkEmailBatchSize { get; init; } = 50;
    
    public int BulkEmailDelayBetweenBatches { get; init; } = 100;
    
    public bool LogEmailContent { get; init; } = false;
    
    public List<string> AllowedDomains { get; init; } = [];
    
    public List<string> BlockedDomains { get; init; } = [];
}