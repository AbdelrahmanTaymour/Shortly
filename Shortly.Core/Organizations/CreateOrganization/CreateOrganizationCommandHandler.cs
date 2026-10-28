using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Entities;
using Shortly.Domain.Members;
using Shortly.Domain.RepositoryContract.Members;
using Shortly.Domain.RepositoryContract.Organizations;
using Shortly.Domain.RepositoryContract.Users;
using Shortly.Domain.Users;

namespace Shortly.Core.Organizations.CreateOrganization;

internal sealed class CreateOrganizationCommandHandler(
    IUserContext userContext,
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    IJwtService jwtService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateOrganizationCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken: cancellationToken);
        if (user is null)
            return Result.Failure<AccessTokenResponse>(AuthenticationErrors.NotAuthenticated);

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            OwnerId = userContext.UserId,
            CreatedAt = DateTime.UtcNow
        };
        organizationRepository.Add(organization);

        var membership = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            UserId = userContext.UserId,
            Role = MemberRole.OrgOwner,
            InvitedBy = userContext.UserId,
            JoinedAt = DateTime.UtcNow
        };
        memberRepository.Add(membership);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await jwtService.IssueTokensAsync(user, membership, cancellationToken);
    }
}