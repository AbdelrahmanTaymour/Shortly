using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Account.Logout;

internal sealed class LogoutCommandHandler(
    IJwtService jwtService,
    ICookieService cookieService) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = cookieService.GetRefreshToken();

        if (!string.IsNullOrEmpty(refreshToken))
            await jwtService.RevokeAsync(refreshToken, cancellationToken);

        cookieService.DeleteRefreshToken();

        return Result.Success();
    }
}