using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Admin.HardDeleteUser;

public sealed record HardDeleteUserCommand(Guid UserId) : ICommand;
