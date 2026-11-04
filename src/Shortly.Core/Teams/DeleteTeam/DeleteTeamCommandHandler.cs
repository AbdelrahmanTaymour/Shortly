using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.DeleteTeam;

internal sealed class DeleteTeamCommandHandler(
    IOrganizationAuthorizationService organizationAuthorization, IOrganizationTeamRepository teamRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteTeamCommand>
{
    public async Task<Result> Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null || team.OrganizationId != request.OrganizationId)
            return Result.Failure(OrganizationTeamErrors.NotFound);

        var caller = await organizationAuthorization.RequireRoleAsync(team.OrganizationId, MemberRole.OrgAdmin, cancellationToken);
        if (caller.IsFailure) return Result.Failure(caller.Error);

        teamRepository.Remove(team);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}