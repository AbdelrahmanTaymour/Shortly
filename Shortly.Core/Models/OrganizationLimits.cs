namespace Shortly.Core.Models;

public record OrganizationLimits
{
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public int MaxLinksPerMonth { get; init; }
    public int MaxQrCodesPerMonth { get; init; }
}