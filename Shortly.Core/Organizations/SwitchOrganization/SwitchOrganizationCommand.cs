using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Organizations.SwitchOrganization;

public sealed record SwitchOrganizationCommand(Guid OrganizationId) : ICommand<AccessTokenResponse>;