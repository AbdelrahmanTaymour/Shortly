using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Organizations.UpdateOrganization;

public sealed record UpdateOrganizationCommand(
    Guid OrganizationId, string? Name, string? Description, string? Website, string? LogoUrl, int? MemberLimit) : ICommand;