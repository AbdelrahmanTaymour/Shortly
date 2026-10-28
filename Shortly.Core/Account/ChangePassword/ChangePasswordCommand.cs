using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Account.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand;