using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.UpdateTeam;

internal sealed class UpdateTeamCommandHandler(
    IOrganizationAuthorizationService organizationAuthorization,
    IOrganizationTeamRepository teamRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateTeamCommand>
{
    public async Task<Result> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null || team.OrganizationId != request.OrganizationId)
            return Result.Failure(OrganizationTeamErrors.NameTaken); // Or OrganizationTeamErrors.NotFound (keeping original error semantics)

        var authority = await TeamAuthorization.RequireAuthorityAsync(organizationAuthorization, team, cancellationToken);
        if (authority.IsFailure) 
            return Result.Failure(authority.Error);

        if (request.Name is not null)
        {
            // Allow case-only changes or identical names, but reject conflicts with other teams
            var isRename = !string.Equals(team.Name, request.Name, StringComparison.OrdinalIgnoreCase);
            if (isRename && await teamRepository.IsTeamNameExistAsync(request.Name, team.OrganizationId, team.Id, cancellationToken))
                return Result.Failure(OrganizationTeamErrors.NameTaken);

            var renamed = team.Rename(request.Name);
            if (renamed.IsFailure) return renamed;
        }

        if (request.Description is not null) team.UpdateDescription(request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}