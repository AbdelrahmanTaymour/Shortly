using System.ComponentModel.DataAnnotations;
using Shortly.Domain.Enums;

namespace Shortly.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name {get; set;}
    public string Email { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public enSubscriptionPlan SubscriptionPlan { get; set; } = enSubscriptionPlan.Free;
    public enUserRole Role { get; set; } = enUserRole.StandardUser;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}