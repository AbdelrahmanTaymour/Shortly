using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Account.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : ICommand;