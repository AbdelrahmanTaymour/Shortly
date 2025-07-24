using Shortly.Domain.Enums;

namespace Shortly.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public enSubscriptionPlan SubscriptionPlan { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // TODO: public Dictionary<string, object> Settings { get; set; } = new();
    
    
    // Navigation properties
    public User Owner {get; set;}
    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
}