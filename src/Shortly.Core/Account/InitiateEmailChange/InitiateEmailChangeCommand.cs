using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Account.InitiateEmailChange;

public sealed record InitiateEmailChangeCommand(string NewEmail, string CurrentPassword) : ICommand;