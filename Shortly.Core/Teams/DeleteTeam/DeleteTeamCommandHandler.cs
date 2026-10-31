using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.DeleteTeam;

internal sealed class DeleteTeamCommandHandler(
    IUserContext userContext, IOrganizationMemberRepository memberRepository, IOrganizationTeamRepository teamRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteTeamCommand>
{
    public async Task<Result> Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null) return Result.Failure(OrganizationTeamErrors.NotFound);

        var caller = await memberRepository.GetByOrganizationAndUserAsync(team.OrganizationId, userContext.UserId, cancellationToken);
        if (caller is null || caller.Role < MemberRole.OrgAdmin)
            return Result.Failure(OrganizationTeamErrors.InsufficientRole);

        teamRepository.Remove(team);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}