using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;

namespace Shortly.Core.Teams.CreateTeam;

internal sealed class CreateTeamCommandHandler(
    IUserContext userContext,
    IOrganizationMemberRepository memberRepository,
    IOrganizationTeamRepository teamRepository,
    IOrganizationTeamMemberRepository teamMemberRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateTeamCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var caller = await memberRepository.GetByOrganizationAndUserAsync(request.OrganizationId, userContext.UserId, cancellationToken);
        if (caller is null || caller.Role < MemberRole.OrgAdmin)
            return Result.Failure<Guid>(OrganizationTeamErrors.InsufficientRole);

        if (!await memberRepository.IsMemberOfOrganizationAsync(request.TeamManagerId, request.OrganizationId, cancellationToken))
            return Result.Failure<Guid>(OrganizationTeamErrors.ManagerNotOrgMember);

        if (await teamRepository.IsTeamNameExistAsync(request.Name, request.OrganizationId, cancellationToken))
            return Result.Failure<Guid>(OrganizationTeamErrors.NameTaken);

        var utcNow = dateTimeProvider.UtcNow;
        var team = OrganizationTeam.Create(request.OrganizationId, request.TeamManagerId, request.Name, request.Description, utcNow);
        teamRepository.Add(team);

        teamMemberRepository.Add(OrganizationTeamMember.Create(team.Id, request.TeamManagerId, utcNow));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return team.Id;
    }
}