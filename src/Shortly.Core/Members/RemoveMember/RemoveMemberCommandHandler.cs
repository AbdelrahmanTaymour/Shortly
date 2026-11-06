using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Core.Members.RemoveMember;

internal sealed class RemoveMemberCommandHandler(
    IOrganizationAuthorizationService organizationAuthorization,
    IOrganizationMemberRepository memberRepository,
    IOrganizationTeamRepository teamRepository,
    IOrganizationTeamMemberRepository teamMemberRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveMemberCommand>
{
    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var caller = await organizationAuthorization.RequireRoleAsync(request.OrganizationId, MemberRole.OrgAdmin, cancellationToken);
        if (caller.IsFailure) return Result.Failure(caller.Error);

        var target = await memberRepository.GetByOrganizationAndUserAsync(request.OrganizationId, request.UserId, cancellationToken);
        if (target is null) return Result.Failure(OrganizationMemberErrors.NotFound);

        var result = target.Deactivate(); // guards OrgOwner internally — no separate "is owner" check needed
        if (result.IsFailure) return result;

        // A team cannot be left without a manager, and the removed member must not stay on any team.
        if (await teamRepository.IsManagerOfAnyTeamAsync(target.Id, cancellationToken))
            return Result.Failure(OrganizationTeamErrors.MemberManagesTeams);

        foreach (var teamMembership in await teamMemberRepository.GetByMemberAsync(target.Id, cancellationToken))
            teamMemberRepository.Remove(teamMembership);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}