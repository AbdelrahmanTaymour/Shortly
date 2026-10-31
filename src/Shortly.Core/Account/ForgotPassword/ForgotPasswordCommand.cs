using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Account.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand;