namespace Shortly.Core.Users.GetMyQuotaStatus;

public sealed record QuotaStatusResponse(
    int MonthlyLinksCreated, 
    int MonthlyLinksLimit, 
    int RemainingLinks, 
    bool CanCreateMoreLinks,
    int MonthlyQrCodesCreated, 
    int MonthlyQrCodesLimit, 
    int RemainingQrCodes,
    bool CanCreateMoreQrCodes,
    DateTime MonthlyResetDateUtc);
