using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Organizations.TransferOwnership;

public sealed record TransferOwnershipCommand(Guid OrganizationId, Guid NewOwnerId) : ICommand;
