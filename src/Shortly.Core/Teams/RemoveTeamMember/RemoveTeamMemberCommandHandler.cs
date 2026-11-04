using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Teams.AddTeamMember;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.RemoveTeamMember;

internal sealed class RemoveTeamMemberCommandHandler(
    IOrganizationAuthorizationService organizationAuthorization,
    IOrganizationTeamRepository teamRepository,
    IOrganizationTeamMemberRepository teamMemberRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveTeamMemberCommand>
{
    public async Task<Result> Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null || team.OrganizationId != request.OrganizationId)
            return Result.Failure(OrganizationTeamErrors.NotFound);

        var authority = await TeamAuthorization.RequireAuthorityAsync(organizationAuthorization, team, cancellationToken);
        if (authority.IsFailure) return Result.Failure(authority.Error);

        // request.MemberId is already an OrganizationMember.Id — direct comparison/lookup, no resolution needed.
        if (request.MemberId == team.TeamManagerId)
            return Result.Failure(OrganizationTeamErrors.CannotRemoveManager);

        var membership = await teamMemberRepository.GetByTeamAndMemberAsync(request.TeamId, request.MemberId, cancellationToken);
        if (membership is null) return Result.Failure(OrganizationTeamErrors.MemberNotFound);

        teamMemberRepository.Remove(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}