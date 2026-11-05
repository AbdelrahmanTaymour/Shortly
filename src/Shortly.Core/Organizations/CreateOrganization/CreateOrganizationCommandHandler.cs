using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Core.Organizations.CreateOrganization;

internal sealed class CreateOrganizationCommandHandler(
    IUserContext userContext,
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    IJwtService jwtService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateOrganizationCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken: cancellationToken);
        if (user is null)
            return Result.Failure<AccessTokenResponse>(AuthenticationErrors.NotAuthenticated);

        var utcNow = dateTimeProvider.UtcNow;
        var created = Organization.Create(
            request.Name, request.Description, request.Website, request.LogoUrl,
            request.MemberLimit ?? 10, userContext.UserId, enSubscriptionPlan.Free, utcNow);
        if (created.IsFailure) return Result.Failure<AccessTokenResponse>(created.Error);

        var organization = created.Value;
        organizationRepository.Add(organization);

        var membership = OrganizationMember.CreateOwner(organization.Id, userContext.UserId, utcNow);
        memberRepository.Add(membership);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await jwtService.IssueTokensAsync(user, membership, cancellationToken);
    }
}