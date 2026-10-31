using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Account.RefreshAccessToken;

public sealed record RefreshAccessTokenCommand : ICommand<AccessTokenResponse>;