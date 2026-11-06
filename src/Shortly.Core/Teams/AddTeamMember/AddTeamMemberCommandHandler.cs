using Shortly.Core.Exceptions;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.AddTeamMember;

internal sealed class AddTeamMemberCommandHandler(
    IOrganizationAuthorizationService organizationAuthorization,
    IOrganizationMemberRepository memberRepository,
    IOrganizationTeamRepository teamRepository,
    IOrganizationTeamMemberRepository teamMemberRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AddTeamMemberCommand>
{
    private const string TeamMemberIndex = "ux_team_members_team_id_member_id";

    public async Task<Result> Handle(AddTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null || team.OrganizationId != request.OrganizationId)
            return Result.Failure(OrganizationTeamErrors.NotFound);

        var authority = await TeamAuthorization.RequireAuthorityAsync(organizationAuthorization, team, cancellationToken);
        if (authority.IsFailure) return Result.Failure(authority.Error);

        var targetMembership = await memberRepository.GetByIdAsync(request.MemberId, cancellationToken);
        if (targetMembership is not { IsActive: true } || targetMembership.OrganizationId != team.OrganizationId)
            return Result.Failure(OrganizationTeamErrors.MemberNotOrgMember);

        if (await teamMemberRepository.IsMemberOfTeamAsync(targetMembership.Id, request.TeamId, cancellationToken))
            return Result.Failure(OrganizationTeamErrors.AlreadyTeamMember);

        teamMemberRepository.Add(OrganizationTeamMember.Create(request.TeamId, targetMembership.Id, dateTimeProvider.UtcNow));
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == TeamMemberIndex)
        {
            // Another request added the same member between the check and the insert.
            return Result.Failure(OrganizationTeamErrors.AlreadyTeamMember);
        }

        return Result.Success();
    }
}
