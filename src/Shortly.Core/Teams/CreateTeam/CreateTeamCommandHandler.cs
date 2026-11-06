using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.CreateTeam;

internal sealed class CreateTeamCommandHandler(
    IOrganizationAuthorizationService organizationAuthorization,
    IOrganizationMemberRepository memberRepository,
    IOrganizationTeamRepository teamRepository,
    IOrganizationTeamMemberRepository teamMemberRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateTeamCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var caller = await organizationAuthorization.RequireRoleAsync(request.OrganizationId, MemberRole.OrgAdmin, cancellationToken);
        if (caller.IsFailure) return Result.Failure<Guid>(caller.Error);

        var managerMembership = await memberRepository.GetByIdAsync(request.TeamManagerId, cancellationToken);
        if (managerMembership is not { IsActive: true } || managerMembership.OrganizationId != request.OrganizationId)
            return Result.Failure<Guid>(OrganizationTeamErrors.ManagerNotOrgMember);

        if (managerMembership.Role < MemberRole.TeamManager)
            return Result.Failure<Guid>(OrganizationTeamErrors.ManagerRoleTooLow);

        if (await teamRepository.IsTeamNameExistAsync(request.Name, request.OrganizationId, cancellationToken: cancellationToken))
            return Result.Failure<Guid>(OrganizationTeamErrors.NameTaken);

        var utcNow = dateTimeProvider.UtcNow;
        var created = OrganizationTeam.Create(request.OrganizationId, managerMembership.Id, request.Name, request.Description, utcNow);
        if (created.IsFailure) return Result.Failure<Guid>(created.Error);

        var team = created.Value;
        teamRepository.Add(team);

        teamMemberRepository.Add(OrganizationTeamMember.Create(team.Id, managerMembership.Id, utcNow));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return team.Id;
    }
}