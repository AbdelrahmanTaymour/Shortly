using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.ChangeTeamManager;

internal sealed class ChangeTeamManagerCommandHandler(
    IOrganizationAuthorizationService organizationAuthorization,
    IOrganizationMemberRepository memberRepository,
    IOrganizationTeamRepository teamRepository,
    IOrganizationTeamMemberRepository teamMemberRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ChangeTeamManagerCommand>
{
    public async Task<Result> Handle(ChangeTeamManagerCommand request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null || team.OrganizationId != request.OrganizationId)
            return Result.Failure(OrganizationTeamErrors.NotFound);

        var caller = await organizationAuthorization.RequireRoleAsync(team.OrganizationId, MemberRole.OrgAdmin, cancellationToken);
        if (caller.IsFailure) return Result.Failure(caller.Error);

        var newManagerMembership = await memberRepository.GetByIdAsync(request.NewManagerId, cancellationToken);
        if (newManagerMembership is not { IsActive: true } || newManagerMembership.OrganizationId != team.OrganizationId)
            return Result.Failure(OrganizationTeamErrors.ManagerNotOrgMember);

        if (newManagerMembership.Role < MemberRole.TeamManager)
            return Result.Failure(OrganizationTeamErrors.ManagerRoleTooLow);

        var changed = team.ChangeManager(newManagerMembership.Id);
        if (changed.IsFailure) return changed;

        if (!await teamMemberRepository.IsMemberOfTeamAsync(newManagerMembership.Id, request.TeamId, cancellationToken))
            teamMemberRepository.Add(OrganizationTeamMember.Create(request.TeamId, newManagerMembership.Id, dateTimeProvider.UtcNow));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}