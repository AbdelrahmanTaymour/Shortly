using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.RepositoryContract.Members;
using Shortly.Domain.Users;

namespace Shortly.Core.Organizations.SwitchOrganization;

internal sealed class SwitchOrganizationCommandHandler(
    IUserContext userContext,
    IUserRepository userRepository,
    IOrganizationMemberRepository memberRepository,
    IJwtService jwtService)
    : ICommandHandler<SwitchOrganizationCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(SwitchOrganizationCommand request, CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;
        
        var membership = await memberRepository.GetByOrganizationAndUserAsync(
            request.OrganizationId, userId, cancellationToken);

        if (membership is null || !membership.IsActive)
            return Result.Failure<AccessTokenResponse>(OrganizationErrors.NotAMember);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user is null)
            return Result.Failure<AccessTokenResponse>(AuthenticationErrors.NotAuthenticated);

        return await jwtService.IssueTokensAsync(user, membership, cancellationToken);
    }
}