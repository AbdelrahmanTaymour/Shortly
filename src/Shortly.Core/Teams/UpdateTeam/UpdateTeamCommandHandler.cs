using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.UpdateTeam;

internal sealed class UpdateTeamCommandHandler(
    IUserContext userContext, IOrganizationMemberRepository memberRepository, IOrganizationTeamRepository teamRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateTeamCommand>
{
    public async Task<Result> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null || team.OrganizationId != request.OrganizationId)
            return Result.Failure(OrganizationTeamErrors.NotFound); // deliberately NotFound, not a separate "mismatch" error — don't confirm the team exists elsewhere

        if (!await CallerHasTeamAuthorityAsync(memberRepository, team, userContext.UserId, cancellationToken))
            return Result.Failure(OrganizationTeamErrors.InsufficientRole);

        if (request.Name is not null) team.Rename(request.Name);
        if (request.Description is not null) team.UpdateDescription(request.Description);

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