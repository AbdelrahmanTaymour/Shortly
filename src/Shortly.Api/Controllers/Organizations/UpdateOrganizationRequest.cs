namespace Shortly.API.Controllers.Organizations;

public sealed record UpdateOrganizationRequest(string? Name, string? Description, string? Website, string? LogoUrl, int? MemberLimit);
