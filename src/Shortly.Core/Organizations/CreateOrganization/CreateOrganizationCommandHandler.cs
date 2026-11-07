using Shortly.Core.Exceptions;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Cache;
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
    ISubscriptionPlanLimitsProvider planLimitsProvider,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateOrganizationCommand, AccessTokenResponse>
{
    private const string OrganizationNameIndex = "ux_organizations_owner_id_name";

    public async Task<Result<AccessTokenResponse>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken: cancellationToken);
        if (user is null)
            return Result.Failure<AccessTokenResponse>(AuthenticationErrors.NotAuthenticated);

        // Requests of the same user wait for each other, so the count below cannot be passed by two requests at once.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await unitOfWork.LockAsync($"organization-create:{user.Id}", cancellationToken);

        // How many organizations a user may own depends on the user's own plan.
        var userLimits = await planLimitsProvider.GetLimitsAsync(user.SubscriptionPlanId, cancellationToken);
        if (!userLimits.HasUnlimitedOrganizations &&
            await organizationRepository.CountOwnedByAsync(user.Id, cancellationToken) >= userLimits.MaxOrganizations)
            return Result.Failure<AccessTokenResponse>(OrganizationErrors.OrganizationLimitReached);

        // A new organization starts on the Free plan, and its member limit follows that plan.
        var organizationLimits = await planLimitsProvider.GetLimitsAsync(enSubscriptionPlan.Free, cancellationToken);

        var utcNow = dateTimeProvider.UtcNow;
        var created = Organization.Create(
            request.Name, request.Description, request.Website, request.LogoUrl,
            organizationLimits.OrganizationMemberLimit, userContext.UserId, enSubscriptionPlan.Free, utcNow);
        if (created.IsFailure) return Result.Failure<AccessTokenResponse>(created.Error);

        var organization = created.Value;
        organizationRepository.Add(organization);

        var membership = OrganizationMember.CreateOwner(organization.Id, userContext.UserId, utcNow);
        memberRepository.Add(membership);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == OrganizationNameIndex)
        {
            return Result.Failure<AccessTokenResponse>(OrganizationErrors.NameTaken);
        }

        await transaction.CommitAsync(cancellationToken);

        return await jwtService.IssueTokensAsync(user, membership, cancellationToken);
    }
}