using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.CheckTeamNameAvailability;

internal sealed class CheckTeamNameAvailabilityCommandHandler(
    IOrganizationAuthorizationService organizationAuthorization,
    IOrganizationTeamRepository teamRepository)
    : ICommandHandler<CheckTeamNameAvailabilityCommand, bool>
{
    public async Task<Result<bool>> Handle(CheckTeamNameAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var caller = await organizationAuthorization.RequireMembershipAsync(request.OrganizationId, cancellationToken);
        if (caller.IsFailure) return Result.Failure<bool>(caller.Error);

        var taken = await teamRepository.IsTeamNameExistAsync(
            request.Name, request.OrganizationId, request.ExcludeTeamId, cancellationToken);

        return !taken;
    }
}
