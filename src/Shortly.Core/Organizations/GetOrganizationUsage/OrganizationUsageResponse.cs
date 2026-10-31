namespace Shortly.Core.Organizations.GetOrganizationUsage;

public sealed record OrganizationUsageResponse(
    int MonthlyLinksCreated, int MonthlyLinksLimit, int RemainingLinks, bool CanCreateMoreLinks,
    int MonthlyQrCodesCreated, int MonthlyQrCodesLimit, int RemainingQrCodes, bool CanCreateMoreQrCodes,
    DateTime MonthlyResetDateUtc);
