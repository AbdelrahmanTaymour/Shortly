namespace Shortly.Core.Models;

public class EmailRequest
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsHtml { get; set; }
    public List<string> Cc { get; set; }
    public List<string> Bcc { get; set; }
}