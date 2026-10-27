using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Domain.Users;

namespace Shortly.Core.Organizations.ExitOrganization;

internal sealed class ExitOrganizationCommandHandler(
    IUserContext userContext,
    IUserRepository userRepository,
    IJwtService jwtService)
    : ICommandHandler<ExitOrganizationCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(ExitOrganizationCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken: cancellationToken);
        if (user is null)
            return Result.Failure<AccessTokenResponse>(AuthenticationErrors.NotAuthenticated);

        return await jwtService.IssueTokensAsync(user, cancellationToken);
    }
}