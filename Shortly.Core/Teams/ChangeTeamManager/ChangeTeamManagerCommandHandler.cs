using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.ChangeTeamManager;

internal sealed class ChangeTeamManagerCommandHandler(
    IUserContext userContext,
    IOrganizationMemberRepository memberRepository,
    IOrganizationTeamRepository teamRepository,
    IOrganizationTeamMemberRepository teamMemberRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ChangeTeamManagerCommand>
{
    public async Task<Result> Handle(ChangeTeamManagerCommand request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null) return Result.Failure(OrganizationTeamErrors.NotFound);

        var caller =
            await memberRepository.GetByOrganizationAndUserAsync(team.OrganizationId, userContext.UserId,
                cancellationToken);
        if (caller is null || caller.Role < MemberRole.OrgAdmin)
            return Result.Failure(OrganizationTeamErrors.InsufficientRole);

        if (!await memberRepository.IsMemberOfOrganizationAsync(request.NewManagerId, team.OrganizationId,
                cancellationToken))
            return Result.Failure(OrganizationTeamErrors.ManagerNotOrgMember);

        team.ChangeManager(request.NewManagerId);

        if (!await teamMemberRepository.IsMemberOfTeamAsync(request.NewManagerId, request.TeamId, cancellationToken))
            teamMemberRepository.Add(OrganizationTeamMember.Create(request.TeamId, request.NewManagerId,
                dateTimeProvider.UtcNow));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}