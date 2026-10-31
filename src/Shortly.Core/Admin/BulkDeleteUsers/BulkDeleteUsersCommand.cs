using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Admin.BulkDeleteUsers;

public sealed record BulkDeleteUsersCommand(IReadOnlyCollection<Guid> UserIds) : ICommand<BulkOperationResponse>;