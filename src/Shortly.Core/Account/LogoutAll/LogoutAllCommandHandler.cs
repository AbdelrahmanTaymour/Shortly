using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Account.LogoutAll;

internal sealed class LogoutAllCommandHandler(
    IUserContext userContext,
    IJwtService jwtService,
    ICookieService cookieService) : ICommandHandler<LogoutAllCommand>
{
    public async Task<Result> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
    {
        await jwtService.RevokeAllForUserAsync(userContext.UserId, cancellationToken);

        cookieService.DeleteRefreshToken();

        return Result.Success();
    }
}