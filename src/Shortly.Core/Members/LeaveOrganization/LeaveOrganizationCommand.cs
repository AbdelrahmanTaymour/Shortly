using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Members.LeaveOrganization;

/// <summary>The caller leaves an organization. The result carries tokens for their personal workspace.</summary>
public sealed record LeaveOrganizationCommand(Guid OrganizationId) : ICommand<AccessTokenResponse>;
