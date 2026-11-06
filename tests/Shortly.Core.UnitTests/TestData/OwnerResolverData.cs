using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Cache;
using Shortly.Core.ShortUrls;
using Shortly.Domain.Organizations;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.TestData;

/// <summary>
/// <see cref="ShortUrlOwnerResolver"/> is a concrete class, so handler tests build a real one over mocked ports and
/// control it through the plan limits provider.
/// </summary>
internal sealed class OwnerResolverData
{
    public IUserRepository Users { get; } = Substitute.For<IUserRepository>();
    public IOrganizationRepository Organizations { get; } = Substitute.For<IOrganizationRepository>();
    public IOrganizationAuthorizationService Authorization { get; } = Substitute.For<IOrganizationAuthorizationService>();
    public ISubscriptionPlanLimitsProvider Plans { get; } = Substitute.For<ISubscriptionPlanLimitsProvider>();

    public ShortUrlOwnerResolver Create(IUserContext caller) =>
        new(caller, Users, Organizations, Authorization, Plans);

    /// <summary>Every plan resolves to the given limits.</summary>
    public OwnerResolverData WithLimits(SubscriptionPlanLimits limits)
    {
        Plans.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>()).Returns(limits);
        return this;
    }

    /// <summary>The owner of a link exists with the given plan.</summary>
    public OwnerResolverData WithUserOwner(User owner)
    {
        Users.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);
        return this;
    }
}
