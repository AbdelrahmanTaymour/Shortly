using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;
using Shortly.Domain.Users;

namespace Shortly.Core.Members.LeaveOrganization;

internal sealed class LeaveOrganizationCommandHandler(
    IUserContext userContext,
    IUserRepository userRepository,
    IOrganizationMemberRepository memberRepository,
    IOrganizationTeamRepository teamRepository,
    IOrganizationTeamMemberRepository teamMemberRepository,
    IJwtService jwtService,
    IUnitOfWork unitOfWork) : ICommandHandler<LeaveOrganizationCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(LeaveOrganizationCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken);
        if (user is null) 
            return Result.Failure<AccessTokenResponse>(AuthenticationErrors.NotAuthenticated);

        var membership = await memberRepository.GetByOrganizationAndUserAsync(request.OrganizationId, user.Id, cancellationToken);
        if (membership is not { IsActive: true })
            return Result.Failure<AccessTokenResponse>(OrganizationErrors.NotAMember);

        if (membership.Role == MemberRole.OrgOwner)
            return Result.Failure<AccessTokenResponse>(OrganizationMemberErrors.OwnerCannotLeave);

        // Operational Guard: Team managers cannot leave until their teams are reassigned
        if (await teamRepository.IsManagerOfAnyTeamAsync(membership.Id, cancellationToken))
            return Result.Failure<AccessTokenResponse>(OrganizationTeamErrors.MemberManagesTeams);

        var deactivated = membership.Deactivate();
        if (deactivated.IsFailure) 
            return Result.Failure<AccessTokenResponse>(deactivated.Error);

        // Remove the member from all assigned teams
        foreach (var teamMembership in await teamMemberRepository.GetByMemberAsync(membership.Id, cancellationToken))
            teamMemberRepository.Remove(teamMembership);

        // Persist all changes atomically
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Re-issue tokens scoped to the user's personal workspace since they left the organization
        return await jwtService.IssueTokensAsync(user, cancellationToken);
    }
}