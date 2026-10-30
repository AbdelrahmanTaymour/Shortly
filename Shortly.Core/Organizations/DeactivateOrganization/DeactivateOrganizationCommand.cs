using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Organizations.DeactivateOrganization;

public sealed record DeactivateOrganizationCommand(Guid OrganizationId) : ICommand;
