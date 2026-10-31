using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Account.ConfirmEmailChange;

public sealed record ConfirmEmailChangeCommand(string Token) : ICommand;