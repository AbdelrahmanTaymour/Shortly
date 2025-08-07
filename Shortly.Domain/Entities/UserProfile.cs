namespace Shortly.Domain.Entities;

public class UserProfile
{
    public required Guid UserId { get; set; }
    public string? Name { get; set; }
    public string? Bio { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Website { get; set; }
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? TimeZone { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
}