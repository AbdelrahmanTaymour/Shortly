using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Account.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<AccessTokenResponse>;