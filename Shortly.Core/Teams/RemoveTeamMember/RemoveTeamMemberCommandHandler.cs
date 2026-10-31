using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.RemoveTeamMember;

internal sealed class RemoveTeamMemberCommandHandler(
    IUserContext userContext,
    IOrganizationMemberRepository memberRepository,
    IOrganizationTeamRepository teamRepository,
    IOrganizationTeamMemberRepository teamMemberRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveTeamMemberCommand>
{
    public async Task<Result> Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null) return Result.Failure(OrganizationTeamErrors.NotFound);

        if (!await CallerHasTeamAuthorityAsync(memberRepository, team, userContext.UserId, cancellationToken))
            return Result.Failure(OrganizationTeamErrors.InsufficientRole);

        if (request.MemberId == team.TeamManagerId)
            return Result.Failure(OrganizationTeamErrors.CannotRemoveManager);

        var membership = await teamMemberRepository.GetByTeamAndMemberAsync(request.TeamId, request.MemberId, cancellationToken);
        if (membership is null) return Result.Failure(OrganizationTeamErrors.MemberNotFound);

        teamMemberRepository.Remove(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
    
    private static async Task<bool> CallerHasTeamAuthorityAsync(
        IOrganizationMemberRepository memberRepository, OrganizationTeam team, Guid callerId, CancellationToken cancellationToken)
    {
        if (team.TeamManagerId == callerId) return true; // the team's own manager can administer it
        var caller = await memberRepository.GetByOrganizationAndUserAsync(team.OrganizationId, callerId, cancellationToken);
        return caller is not null && caller.Role >= MemberRole.OrgAdmin;
    }
}