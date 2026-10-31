namespace Shortly.Infrastructure.Email;

internal sealed class EmailRequest
{
    public string To { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public bool IsHtml { get; init; }
}