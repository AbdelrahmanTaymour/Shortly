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
        if (team is null || team.OrganizationId != request.OrganizationId)
            return Result.Failure(OrganizationTeamErrors.NotFound);

        if (!await CallerHasTeamAuthorityAsync(memberRepository, team, userContext.UserId, cancellationToken))
            return Result.Failure(OrganizationTeamErrors.InsufficientRole);

        var targetMembership = await memberRepository.GetByIdAsync(request.MemberId, cancellationToken);
        if (targetMembership is null || targetMembership.OrganizationId != team.OrganizationId)
            return Result.Failure(OrganizationTeamErrors.MemberNotOrgMember);

        if (await teamMemberRepository.IsMemberOfTeamAsync(targetMembership.Id, request.TeamId, cancellationToken))
            return Result.Failure(OrganizationTeamErrors.AlreadyTeamMember);

        teamMemberRepository.Add(OrganizationTeamMember.Create(request.TeamId, targetMembership.Id, dateTimeProvider.UtcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    internal static async Task<bool> CallerHasTeamAuthorityAsync(
        IOrganizationMemberRepository memberRepository, OrganizationTeam team, Guid callerUserId, CancellationToken cancellationToken)
    {
        var caller = await memberRepository.GetByOrganizationAndUserAsync(team.OrganizationId, callerUserId, cancellationToken);
        if (caller is null) return false;
        return caller.Id == team.TeamManagerId || caller.Role >= MemberRole.OrgAdmin;
    }
}