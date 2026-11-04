using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Teams.AddTeamMember;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
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
            return Result.Failure(OrganizationTeamErrors.NotFound);

        var authority = await TeamAuthorization.RequireAuthorityAsync(organizationAuthorization, team, cancellationToken);
        if (authority.IsFailure) return Result.Failure(authority.Error);

        if (request.Name is not null) team.Rename(request.Name);
        if (request.Description is not null) team.UpdateDescription(request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}