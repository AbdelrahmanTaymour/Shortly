using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Admin.UnlockUser;

public sealed record UnlockUserCommand(Guid UserId) : ICommand;
