using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Account.Register;

internal sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    ICookieService cookieService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RegisterCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.EmailOrUsernameExistsAsync(request.Email, request.Username, cancellationToken))
            return Result.Failure<AccessTokenResponse>(UserErrors.EmailOrUsernameNotUnique);

        var userResult = User.Register(request.Email, request.Username, passwordHasher.Hash(request.Password), dateTimeProvider.UtcNow);
        if (userResult.IsFailure) 
            return Result.Failure<AccessTokenResponse>(userResult.Error);

        userRepository.Add(userResult.Value);

        var tokensResult = await jwtService.IssueTokensAsync(userResult.Value, cancellationToken);
        if (tokensResult.IsFailure) 
            return tokensResult;

        var utcNow = dateTimeProvider.UtcNow;
        cookieService.SetRefreshToken(tokensResult.Value.RefreshToken, utcNow.AddDays(15));
        
        return tokensResult;
    }
}