using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Account.GoogleLogin;

public sealed record GoogleLoginCommand(string GoogleId, string Email, string? Name, string? Picture)
    : ICommand<AccessTokenResponse>;