namespace Shortly.Core.Entities;

public enum DomainStatus
{
    Pending = 0,
    Verified = 1,
    Failed = 2,
    Suspended = 3
}

public class CustomDomain
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Domain { get; set; } = null!;
    public DomainStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? SslCertificate { get; set; }
    public DateTime? SslExpiresAt { get; set; }
    public bool IsDefault { get; set; }
    public string? VerificationToken { get; set; }
    public string? DnsRecordValue { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<ShortUrl> ShortUrls { get; set; } = new List<ShortUrl>();
}

public class DomainVerification
{
    public int Id { get; set; }
    public int CustomDomainId { get; set; }
    public string VerificationType { get; set; } = null!; // DNS, HTTP, etc.
    public string Token { get; set; } = null!;
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    
    // Navigation properties
    public CustomDomain CustomDomain { get; set; } = null!;
}