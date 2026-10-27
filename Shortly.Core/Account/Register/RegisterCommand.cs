using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Account.Register;

public sealed record RegisterCommand(string Email, string Username, string Password) : ICommand<AccessTokenResponse>;