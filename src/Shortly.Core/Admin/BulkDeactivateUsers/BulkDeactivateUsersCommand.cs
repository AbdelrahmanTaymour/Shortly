using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Admin.BulkDeactivateUsers;

public sealed record BulkDeactivateUsersCommand(IReadOnlyCollection<Guid> UserIds) : ICommand<BulkOperationResponse>;