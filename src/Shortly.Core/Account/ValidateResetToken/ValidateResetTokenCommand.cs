using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Account.ValidateResetToken;

public sealed record ValidateResetTokenCommand(string Token) : ICommand<bool>;