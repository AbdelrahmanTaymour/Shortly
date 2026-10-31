using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Organizations.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Name, string? Description, string? Website, string? LogoUrl, int? MemberLimit) : ICommand<AccessTokenResponse>;