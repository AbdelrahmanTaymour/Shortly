namespace Shortly.Domain.Entities;

/// <summary>
/// Define the ShortUrl class which acts as an entity model class to store Url details in the data store 
/// </summary>
public class ShortUrl
{
    public int Id { get; set; }
    public string OriginalUrl { get; set; }
    public string? ShortCode { get; set; }
    public Guid? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }
    public int AccessCount { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string? ApiKey { get; set; }
    
    // Navigation properties
    public User? User { get; set; }
}