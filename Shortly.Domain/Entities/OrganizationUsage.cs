namespace Shortly.Domain.Entities;

public class OrganizationUsage
{
    public Guid OrganizationId { get; set; }
    public int MonthlyLinksCreated { get; set; }
    public int MonthlyQrCodesCreated { get; set; }
    public int TotalLinksCreated { get; set; }
    public int TotalQrCodesCreated { get; set; }
    public DateTime MonthlyResetDate { get; set; } = DateTime.UtcNow.AddMonths(1);

    // Navigation properties
    public Organization? Organization { get; set; }
}