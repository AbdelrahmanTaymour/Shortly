namespace Shortly.Core.Abstractions.Email;

public record EmailTemplate
{
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public bool IsHtml { get; init; }
}