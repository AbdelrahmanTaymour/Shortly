namespace Shortly.Core.Entities;

public enum WebhookEvent
{
    UrlCreated = 1,
    UrlClicked = 2,
    UrlExpired = 3,
    UrlDeleted = 4,
    DailyReport = 5
}

public class Webhook
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Url { get; set; } = null!;
    public WebhookEvent Event { get; set; }
    public bool IsActive { get; set; }
    public string? Secret { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastTriggered { get; set; }
    public int FailureCount { get; set; }
    public DateTime? LastFailure { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<WebhookDelivery> Deliveries { get; set; } = new List<WebhookDelivery>();
}

public class WebhookDelivery
{
    public int Id { get; set; }
    public int WebhookId { get; set; }
    public string Payload { get; set; } = null!;
    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public DateTime DeliveredAt { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Navigation properties
    public Webhook Webhook { get; set; } = null!;
}

public class WebhookPayload
{
    public string Event { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public object Data { get; set; } = null!;
    public string Signature { get; set; } = null!;
}