namespace Shortly.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string Email { get; set; } 
    public string Username { get; set; }
    public string Password { get; set; } 
    public DateTime CreatedAt { get; set; }
    
    
}