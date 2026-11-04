using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Cache;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Core.ShortUrls;

/// <summary>
/// Resolves link ownership and enforces subscription plan limits, organization status, and monthly quotas.
/// Shared across single and bulk creation to prevent rule drift.
/// </summary>
internal sealed class ShortUrlOwnerResolver(
    IUserContext userContext,
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    IOrganizationAuthorizationService organizationAuthorization,
    ISubscriptionPlanLimitsProvider planLimitsProvider)
{
    /// <summary>Resolves the link owner, validating quotas and feature entitlements.</summary>
    /// <param name="linksRequested">Number of links requested (null if validated later per batch item).</param>
    /// <param name="requireBulkCreation">Ensures the owner's plan supports bulk creation.</param>
    public async Task<Result<ShortUrlOwner>> ResolveAsync(
        int? linksRequested,
        bool requireBulkCreation,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return userContext.OwnerType switch
        {
            enShortUrlOwnerType.User => await ResolveUserOwnerAsync(linksRequested, requireBulkCreation, utcNow, cancellationToken),
            enShortUrlOwnerType.Organization => await ResolveOrganizationOwnerAsync(linksRequested, requireBulkCreation, utcNow, cancellationToken),
            enShortUrlOwnerType.Anonymous => ResolveAnonymousOwner(requireBulkCreation, cancellationToken),
            _ => Result.Failure<ShortUrlOwner>(ShortUrlErrors.InvalidOwnerType)
        };
    }

    /// <summary>Resolves the subscription plan limits applicable to an existing link based on its owner.</summary>
    public async Task<SubscriptionPlanLimits> GetLimitsForLinkAsync(ShortUrl link, CancellationToken cancellationToken)
    {
        var plan = enSubscriptionPlan.Free;

        switch (link.OwnerType)
        {
            case enShortUrlOwnerType.User when link.UserId is { } userId:
                plan = (await userRepository.GetByIdAsync(userId, cancellationToken))?.SubscriptionPlanId ?? enSubscriptionPlan.Free;
                break;
            case enShortUrlOwnerType.Organization when link.OrganizationId is { } organizationId:
                plan = (await organizationRepository.GetByIdAsync(organizationId, cancellationToken))?.SubscriptionPlanId ?? enSubscriptionPlan.Free;
                break;
        }

        return await planLimitsProvider.GetLimitsAsync(plan, cancellationToken);
    }

    private async Task<Result<ShortUrlOwner>> ResolveUserOwnerAsync(
        int? linksRequested, bool requireBulkCreation, DateTime utcNow, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userContext.UserId, cancellationToken);
        if (user is null) return Result.Failure<ShortUrlOwner>(UserErrors.NotFound);

        var limits = await planLimitsProvider.GetLimitsAsync(user.SubscriptionPlanId, cancellationToken);
        var remaining = user.Usage.RemainingMonthlyLinks(limits.MaxLinksPerMonth, utcNow);

        return CheckLimits(limits, remaining, linksRequested, requireBulkCreation)
               ?? new ShortUrlOwner(remaining, limits, User: user);
    }

    private async Task<Result<ShortUrlOwner>> ResolveOrganizationOwnerAsync(
        int? linksRequested, bool requireBulkCreation, DateTime utcNow, CancellationToken cancellationToken)
    {
        var membershipResult = await organizationAuthorization.ResolveClaimedMembershipAsync(cancellationToken);
        if (membershipResult.IsFailure) return Result.Failure<ShortUrlOwner>(membershipResult.Error);
        if (membershipResult.Value is not { } membership) return Result.Failure<ShortUrlOwner>(ShortUrlErrors.InvalidOwnerType);

        // Viewers are read-only and cannot create organization resources
        if (membership.Role < MemberRole.Member)
            return Result.Failure<ShortUrlOwner>(OrganizationMemberErrors.InsufficientRole);

        var organization = await organizationRepository.GetByIdAsync(membership.OrganizationId, cancellationToken);
        if (organization is null) return Result.Failure<ShortUrlOwner>(OrganizationErrors.NotFound);
        if (!organization.IsActive) return Result.Failure<ShortUrlOwner>(OrganizationErrors.Inactive);

        var limits = await planLimitsProvider.GetLimitsAsync(organization.SubscriptionPlanId, cancellationToken);
        var remaining = organization.Usage.RemainingMonthlyLinks(limits.MaxLinksPerMonth, utcNow);

        return CheckLimits(limits, remaining, linksRequested, requireBulkCreation)
               ?? new ShortUrlOwner(remaining, limits, Organization: organization, MemberId: membership.Id);
    }

    private Result<ShortUrlOwner> ResolveAnonymousOwner(bool requireBulkCreation, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userContext.AnonymousSessionId))
            return Result.Failure<ShortUrlOwner>(ShortUrlErrors.InvalidOwnerType);

        if (requireBulkCreation)
            return Result.Failure<ShortUrlOwner>(ShortUrlErrors.BulkCreationNotAllowed);

        // Anonymous callers default to Free plan entitlements
        var freeLimits = planLimitsProvider.GetLimitsAsync(enSubscriptionPlan.Free, cancellationToken).Result;
        return new ShortUrlOwner(int.MaxValue, freeLimits, AnonymousSessionId: userContext.AnonymousSessionId);
    }

    private static Result<ShortUrlOwner>? CheckLimits(
        SubscriptionPlanLimits limits, int remaining, int? linksRequested, bool requireBulkCreation)
    {
        if (requireBulkCreation && !limits.BulkCreation)
            return Result.Failure<ShortUrlOwner>(ShortUrlErrors.BulkCreationNotAllowed);

        if (linksRequested is { } requested && remaining < requested)
            return Result.Failure<ShortUrlOwner>(ShortUrlErrors.MonthlyLinkLimitReached);

        return null;
    }
}

/// <summary>Represents the resolved owner context for a new short URL.</summary>
internal sealed record ShortUrlOwner(
    int RemainingMonthlyLinks,
    SubscriptionPlanLimits Limits,
    User? User = null,
    Organization? Organization = null,
    Guid? MemberId = null,
    string? AnonymousSessionId = null)
{
    public Guid? OrganizationId => Organization?.Id;

    public void RecordLinksCreated(int count, DateTime utcNow)
    {
        for (var i = 0; i < count; i++)
        {
            User?.Usage.IncrementLinksCreated(utcNow);
            Organization?.Usage.IncrementLinksCreated(utcNow);
        }
    }
}
