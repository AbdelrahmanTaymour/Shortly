namespace Shortly.Domain.Organizations;

public sealed class OrganizationUsage
{
    private OrganizationUsage(Guid organizationId, DateTime monthlyResetDate)
    {
        OrganizationId = organizationId;
        MonthlyResetDate = monthlyResetDate;
    }

    private OrganizationUsage() { } // EF

    public Guid OrganizationId { get; private set; }
    public int MonthlyLinksCreated { get; private set; }
    public int MonthlyQrCodesCreated { get; private set; }
    public int TotalLinksCreated { get; private set; }
    public int TotalQrCodesCreated { get; private set; }
    public DateTime MonthlyResetDate { get; private set; }

    internal static OrganizationUsage CreateDefault(Guid organizationId, DateTime utcNow)
        => new(organizationId, utcNow.AddMonths(1));

    public void IncrementLinksCreated(DateTime utcNow)
    {
        ResetIfDue(utcNow);
        MonthlyLinksCreated++;
        TotalLinksCreated++;
    }

    public void IncrementQrCodesCreated(DateTime utcNow)
    {
        ResetIfDue(utcNow);
        MonthlyQrCodesCreated++;
        TotalQrCodesCreated++;
    }

    public void ResetMonthly(DateTime utcNow)
    {
        MonthlyLinksCreated = 0;
        MonthlyQrCodesCreated = 0;
        MonthlyResetDate = utcNow.AddMonths(1);
    }
    
    public bool HasReachedMonthlyLinkLimit(int maxLinksPerMonth, DateTime utcNow)
    {
        if (maxLinksPerMonth < 0) return false; // unlimited plan
        var effectiveCount = utcNow >= MonthlyResetDate ? 0 : MonthlyLinksCreated;
        return effectiveCount >= maxLinksPerMonth;
    }

    private void ResetIfDue(DateTime utcNow)
    {
        if (utcNow < MonthlyResetDate) return;
        ResetMonthly(utcNow);
    }
}