using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Admin.BulkActivateUsers;

public sealed record BulkActivateUsersCommand(IReadOnlyCollection<Guid> UserIds) : ICommand<BulkOperationResponse>;
