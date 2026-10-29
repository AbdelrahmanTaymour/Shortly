using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Admin.LockUser;

public sealed record LockUserCommand(Guid UserId, string Reason, TimeSpan Duration) : ICommand;
