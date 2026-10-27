using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Account.SendEmailVerification;

public sealed record SendEmailVerificationCommand(string Email) : ICommand;