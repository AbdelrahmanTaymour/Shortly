using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Organizations.ExitOrganization;

public sealed record ExitOrganizationCommand : ICommand<AccessTokenResponse>;