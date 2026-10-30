using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Organizations.DeleteOrganization;

public sealed record DeleteOrganizationCommand(Guid OrganizationId) : ICommand;
