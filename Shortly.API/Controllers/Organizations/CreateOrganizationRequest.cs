namespace Shortly.API.Controllers.Organizations;

public sealed record CreateOrganizationRequest(string Name, string? Description, string? Website, string? LogoUrl, int? MemberLimit);