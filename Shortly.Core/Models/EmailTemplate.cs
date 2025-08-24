namespace Shortly.Core.Models;

public record EmailTemplate
{
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsHtml { get; set; }
}