using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Organizations.ActivateOrganization;

public sealed record ActivateOrganizationCommand(Guid OrganizationId) : ICommand;
