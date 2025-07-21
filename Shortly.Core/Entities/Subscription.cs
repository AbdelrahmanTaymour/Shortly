namespace Shortly.Core.Entities;

public enum SubscriptionTier
{
    Free = 0,
    Basic = 1,
    Premium = 2,
    Enterprise = 3
}

public class Subscription
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public SubscriptionTier Tier { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public int MaxUrls { get; set; }
    public int MaxClicksPerMonth { get; set; }
    public bool CanUseCustomDomains { get; set; }
    public bool CanUseAnalytics { get; set; }
    public bool CanUsePasswordProtection { get; set; }
    public bool CanUseExpirationDates { get; set; }
    public bool CanRemoveBranding { get; set; }
    public decimal MonthlyPrice { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
}

public class SubscriptionPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public SubscriptionTier Tier { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public int MaxUrls { get; set; }
    public int MaxClicksPerMonth { get; set; }
    public bool CanUseCustomDomains { get; set; }
    public bool CanUseAnalytics { get; set; }
    public bool CanUsePasswordProtection { get; set; }
    public bool CanUseExpirationDates { get; set; }
    public bool CanRemoveBranding { get; set; }
    public string Features { get; set; } = null!; // JSON string of features
    public bool IsActive { get; set; }
}