namespace Shortly.Core.Admin.SearchOrganizations;

public sealed class OrganizationSummaryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid OwnerId { get; init; }
    public string OwnerEmail { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsSubscribed { get; init; }
    public int MemberCount { get; init; }
    public DateTime CreatedAt { get; init; }
}