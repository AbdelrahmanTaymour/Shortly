namespace Shortly.Core.Admin.SearchOrganizations;

public sealed record OrganizationSummaryResponse(Guid Id, string Name, Guid OwnerId, string OwnerEmail, bool IsActive, bool IsSubscribed, int MemberCount, DateTime CreatedAt);
