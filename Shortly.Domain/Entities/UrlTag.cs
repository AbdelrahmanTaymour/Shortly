namespace Shortly.Domain.Entities;

public class UrlTag
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; } = "#007bff"; // Default blue color
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; }
    public ICollection<ShortUrl> ShortUrls { get; set; } = new List<ShortUrl>();
}