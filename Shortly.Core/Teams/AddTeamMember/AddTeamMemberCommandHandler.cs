using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.AddTeamMember;

internal sealed class AddTeamMemberCommandHandler(
    IUserContext userContext,
    IOrganizationMemberRepository memberRepository,
    IOrganizationTeamRepository teamRepository,
    IOrganizationTeamMemberRepository teamMemberRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AddTeamMemberCommand>
{
    public async Task<Result> Handle(AddTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null) return Result.Failure(OrganizationTeamErrors.NotFound);

        if (!await CallerHasTeamAuthorityAsync(memberRepository, team, userContext.UserId,
                cancellationToken))
            return Result.Failure(OrganizationTeamErrors.InsufficientRole);

        if (!await memberRepository.IsMemberOfOrganizationAsync(request.MemberId, team.OrganizationId,
                cancellationToken))
            return Result.Failure(OrganizationTeamErrors.MemberNotOrgMember);

        if (await teamMemberRepository.IsMemberOfTeamAsync(request.MemberId, request.TeamId, cancellationToken))
            return Result.Failure(OrganizationTeamErrors.AlreadyTeamMember);

        teamMemberRepository.Add(OrganizationTeamMember.Create(request.TeamId, request.MemberId,
            dateTimeProvider.UtcNow));
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