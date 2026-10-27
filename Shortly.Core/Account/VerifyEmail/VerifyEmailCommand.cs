using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Account.VerifyEmail;

public sealed record VerifyEmailCommand(string Token) : ICommand<AccessTokenResponse>;