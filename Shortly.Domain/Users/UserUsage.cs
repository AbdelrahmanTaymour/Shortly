namespace Shortly.Domain.Users;

public sealed class UserUsage
{
    private UserUsage(Guid userId, DateTime monthlyResetDate)
    {
        UserId = userId;
        MonthlyResetDate = monthlyResetDate;
    }

    private UserUsage() { }

    public Guid UserId { get; private set; }
    public int MonthlyLinksCreated { get; private set; }
    public int MonthlyQrCodesCreated { get; private set; }
    public int TotalLinksCreated { get; private set; }
    public int TotalQrCodesCreated { get; private set; }
    public DateTime MonthlyResetDate { get; private set; }

    internal static UserUsage CreateDefault(Guid userId, DateTime utcNow) => new(userId, utcNow.AddMonths(1));

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

    private void ResetIfDue(DateTime utcNow)
    {
        if (utcNow < MonthlyResetDate) return;
        MonthlyLinksCreated = 0;
        MonthlyQrCodesCreated = 0;
        MonthlyResetDate = utcNow.AddMonths(1);
    }
    
    public void ResetMonthly(DateTime utcNow)
    {
        MonthlyLinksCreated = 0;
        MonthlyQrCodesCreated = 0;
        MonthlyResetDate = utcNow.AddMonths(1);
    }
}