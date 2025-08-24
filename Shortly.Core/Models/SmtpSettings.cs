namespace Shortly.Core.Models;

public record SmtpSettings
{
    public string Host { get; set; }
    public int Port { get; set; } = 587;
    public string Username { get; set; }
    public string Password { get; set; }
    public string FromEmail { get; set; }
    public string FromName { get; set; }
    public bool EnableSsl { get; set; } = true;
    public bool UseDefaultCredentials { get; set; } = false;
    public int Timeout { get; set; } = 30000; // 30 seconds
    public string Domain { get; set; }
}