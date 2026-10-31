using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Account.ValidateResetToken;

public sealed record ValidateResetTokenQuery(string Token) : IQuery<bool>;