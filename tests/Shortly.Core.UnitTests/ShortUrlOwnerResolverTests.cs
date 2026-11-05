using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Cache;
using Shortly.Core.ShortUrls;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests;

/// <summary>The rules single and bulk creation share: status, plan entitlement and monthly quota.</summary>
public class ShortUrlOwnerResolverTests
{
    private static readonly DateTime Now = TestData.UtcNow;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IOrganizationAuthorizationService _authorization = Substitute.For<IOrganizationAuthorizationService>();
    private readonly ISubscriptionPlanLimitsProvider _plans = Substitute.For<ISubscriptionPlanLimitsProvider>();

    private static SubscriptionPlanLimits Plan(int maxLinks, bool bulk) =>
        new(maxLinks, 0, 0, false, bulk, false, false, false, false);

    private ShortUrlOwnerResolver Sut(IUserContext context) =>
        new(context, _users, _organizations, _authorization, _plans);

    private IUserContext PersonalCaller()
    {
        var context = TestData.Caller(_userId);
        context.OwnerType.Returns(enShortUrlOwnerType.User);
        return context;
    }

    private User UserWithUsage(int linksThisMonth, enSubscriptionPlan plan = enSubscriptionPlan.Free)
    {
        var user = User.Register($"{Guid.NewGuid():N}@example.com", "name", "hash", Now).Value;
        user.ChangeSubscriptionPlan(plan, Now);
        for (var i = 0; i < linksThisMonth; i++) user.Usage.IncrementLinksCreated(Now);
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    // ---- personal owner ------------------------------------------------------------------------------

    [Fact]
    public async Task User_ReportsRemainingQuota()
    {
        UserWithUsage(linksThisMonth: 3);
        _plans.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>()).Returns(Plan(5, bulk: false));

        var result = await Sut(PersonalCaller()).ResolveAsync(null, false, Now, default);

        result.Value.RemainingMonthlyLinks.Should().Be(2);
    }

    [Fact]
    public async Task User_WithRequestedLinksExceedingQuota_IsRejected()
    {
        UserWithUsage(linksThisMonth: 4);
        _plans.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>()).Returns(Plan(5, bulk: false));

        var result = await Sut(PersonalCaller()).ResolveAsync(2, false, Now, default);

        result.Error.Should().Be(ShortUrlErrors.MonthlyLinkLimitReached);
    }

    [Fact]
    public async Task User_WithRequestedLinksExactlyFillingQuota_IsAccepted()
    {
        UserWithUsage(linksThisMonth: 4);
        _plans.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>()).Returns(Plan(5, bulk: false));

        (await Sut(PersonalCaller()).ResolveAsync(1, false, Now, default)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task User_OnUnlimitedPlan_HasUnboundedRemainingQuota()
    {
        UserWithUsage(linksThisMonth: 100000);
        _plans.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>()).Returns(Plan(-1, bulk: true));

        var result = await Sut(PersonalCaller()).ResolveAsync(500, true, Now, default);

        result.Value.RemainingMonthlyLinks.Should().Be(int.MaxValue);
    }

    [Fact]
    public async Task User_CounterFromAPreviousMonth_DoesNotCountAgainstTheNewMonth()
    {
        UserWithUsage(linksThisMonth: 5); // reset date is Now + 1 month
        _plans.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>()).Returns(Plan(5, bulk: false));

        var result = await Sut(PersonalCaller()).ResolveAsync(null, false, Now.AddMonths(2), default);

        result.Value.RemainingMonthlyLinks.Should().Be(5);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task BulkCreation_RequiresThePlanEntitlement(bool planAllowsBulk, bool expectedSuccess)
    {
        UserWithUsage(linksThisMonth: 0);
        _plans.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>()).Returns(Plan(500, planAllowsBulk));

        var result = await Sut(PersonalCaller()).ResolveAsync(null, requireBulkCreation: true, Now, default);

        result.IsSuccess.Should().Be(expectedSuccess);
        if (!expectedSuccess) result.Error.Should().Be(ShortUrlErrors.BulkCreationNotAllowed);
    }

    [Fact]
    public async Task SingleCreation_DoesNotRequireTheBulkEntitlement()
    {
        UserWithUsage(linksThisMonth: 0);
        _plans.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>()).Returns(Plan(5, bulk: false));

        (await Sut(PersonalCaller()).ResolveAsync(1, requireBulkCreation: false, Now, default)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Anonymous_CannotUseBulkCreation()
    {
        var anonymous = Substitute.For<IUserContext>();
        anonymous.OwnerType.Returns(enShortUrlOwnerType.Anonymous);
        anonymous.AnonymousSessionId.Returns("session");

        var result = await Sut(anonymous).ResolveAsync(null, requireBulkCreation: true, Now, default);

        result.Error.Should().Be(ShortUrlErrors.BulkCreationNotAllowed);
    }

    // ---- organization owner --------------------------------------------------------------------------

    private (IUserContext Caller, Organization Organization) OrganizationCaller(bool active = true, int linksThisMonth = 0)
    {
        var organization = Organization.Create("Acme", null, null, null, 10, Guid.NewGuid(), enSubscriptionPlan.Professional, Now).Value;
        for (var i = 0; i < linksThisMonth; i++) organization.Usage.IncrementLinksCreated(Now);
        if (!active) organization.Deactivate(Now);

        var context = TestData.Caller(_userId, organizationId: organization.Id);
        context.OwnerType.Returns(enShortUrlOwnerType.Organization);

        _authorization.ResolveClaimedMembershipAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ClaimedMembership(TestData.Member(organization.Id, _userId, MemberRole.Member))));
        _organizations.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);
        return (context, organization);
    }

    [Fact]
    public async Task Organization_Inactive_IsRejected()
    {
        var (caller, _) = OrganizationCaller(active: false);
        _plans.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>()).Returns(Plan(500, bulk: true));

        var result = await Sut(caller).ResolveAsync(null, true, Now, default);

        result.Error.Should().Be(OrganizationErrors.Inactive);
    }

    [Fact]
    public async Task Organization_UsesTheDatabaseMemberId_AndReportsOrganizationQuota()
    {
        var (caller, organization) = OrganizationCaller(linksThisMonth: 100);
        _plans.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>()).Returns(Plan(500, bulk: true));

        var result = await Sut(caller).ResolveAsync(null, true, Now, default);

        result.Value.OrganizationId.Should().Be(organization.Id);
        result.Value.MemberId.Should().NotBeNull();
        result.Value.RemainingMonthlyLinks.Should().Be(400);
    }

    [Fact]
    public async Task Organization_WithStaleClaim_IsRejected()
    {
        var (caller, _) = OrganizationCaller();
        _authorization.ResolveClaimedMembershipAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ClaimedMembership>(OrganizationErrors.NotAMember));

        var result = await Sut(caller).ResolveAsync(null, true, Now, default);

        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task RecordLinksCreated_IncrementsUsageByTheCount()
    {
        var (caller, organization) = OrganizationCaller(linksThisMonth: 10);
        _plans.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>()).Returns(Plan(500, bulk: true));
        var owner = (await Sut(caller).ResolveAsync(null, true, Now, default)).Value;

        owner.RecordLinksCreated(7, Now);

        organization.Usage.MonthlyLinksCreated.Should().Be(17);
    }

    // ---- entitlements: which plan applies --------------------------------------------------------------------------

    private static SubscriptionPlanLimits PlanFlags(bool protection = false, bool customCode = false) =>
        new(10, 0, 0, false, false, protection, customCode, false, false);

    [Fact]
    public async Task Anonymous_GetsTheFreePlansEntitlements()
    {
        var free = PlanFlags(protection: false, customCode: false);
        _plans.GetLimitsAsync(enSubscriptionPlan.Free, Arg.Any<CancellationToken>()).Returns(free);
        var anonymous = Substitute.For<IUserContext>();
        anonymous.OwnerType.Returns(enShortUrlOwnerType.Anonymous);
        anonymous.AnonymousSessionId.Returns("session");

        var result = await Sut(anonymous).ResolveAsync(1, false, Now, default);

        result.Value.Limits.Should().BeSameAs(free);
        await _plans.DidNotReceive().GetLimitsAsync(Arg.Is<enSubscriptionPlan>(p => p != enSubscriptionPlan.Free), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolvedOwner_CarriesTheOwnersLimits_NotAnotherPlans()
    {
        var professional = PlanFlags(protection: true, customCode: true);
        UserWithUsage(linksThisMonth: 0, plan: enSubscriptionPlan.Professional);
        _plans.GetLimitsAsync(enSubscriptionPlan.Professional, Arg.Any<CancellationToken>()).Returns(professional);

        var result = await Sut(PersonalCaller()).ResolveAsync(null, false, Now, default);

        result.Value.Limits.Should().BeSameAs(professional);
    }

    private ShortUrl LinkOwnedByUser(Guid userId) => ShortUrl.CreateForUser(
        userId, OriginalUrl.Create("https://example.com").Value, ShortCode.Create("abc1234").Value, Now).Value;

    [Fact]
    public async Task LinkOwnedByAUser_UsesTheOwningUsersPlan_NotTheCallers()
    {
        var owner = User.Register($"{Guid.NewGuid():N}@example.com", "owner", "hash", Now).Value;
        owner.ChangeSubscriptionPlan(enSubscriptionPlan.Starter, Now);
        _users.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);
        var starter = PlanFlags(customCode: true);
        _plans.GetLimitsAsync(enSubscriptionPlan.Starter, Arg.Any<CancellationToken>()).Returns(starter);

        // The caller is a different, Enterprise-plan user: their plan must not be consulted.
        var caller = PersonalCaller();
        var result = await Sut(caller).GetLimitsForLinkAsync(LinkOwnedByUser(owner.Id), default);

        result.Should().BeSameAs(starter);
        await _users.DidNotReceive().GetByIdAsync(_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinkOwnedByAnOrganization_UsesTheOrganizationsPlan()
    {
        var organization = Organization.Create("Acme", null, null, null, 10, Guid.NewGuid(), enSubscriptionPlan.Professional, Now).Value;
        _organizations.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);
        var professional = PlanFlags(protection: true, customCode: true);
        _plans.GetLimitsAsync(enSubscriptionPlan.Professional, Arg.Any<CancellationToken>()).Returns(professional);
        var link = ShortUrl.CreateForOrganization(
            organization.Id, Guid.NewGuid(), OriginalUrl.Create("https://example.com").Value, ShortCode.Create("org1234").Value, Now).Value;

        var result = await Sut(PersonalCaller()).GetLimitsForLinkAsync(link, default);

        result.Should().BeSameAs(professional);
    }

    [Fact]
    public async Task AnonymousLink_AndLinksWhoseOwnerNoLongerExists_AreFree()
    {
        var free = PlanFlags();
        _plans.GetLimitsAsync(enSubscriptionPlan.Free, Arg.Any<CancellationToken>()).Returns(free);
        var anonymousLink = ShortUrl.CreateForAnonymous(
            "session", "203.0.113.1", OriginalUrl.Create("https://example.com").Value, ShortCode.Create("anon123").Value, Now).Value;

        (await Sut(PersonalCaller()).GetLimitsForLinkAsync(anonymousLink, default)).Should().BeSameAs(free);

        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        (await Sut(PersonalCaller()).GetLimitsForLinkAsync(LinkOwnedByUser(Guid.NewGuid()), default)).Should().BeSameAs(free);

        _organizations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Organization?)null);
        var orgLink = ShortUrl.CreateForOrganization(
            Guid.NewGuid(), Guid.NewGuid(), OriginalUrl.Create("https://example.com").Value, ShortCode.Create("org5678").Value, Now).Value;
        (await Sut(PersonalCaller()).GetLimitsForLinkAsync(orgLink, default)).Should().BeSameAs(free);
    }

    [Fact]
    public async Task Organization_Viewer_CannotCreateLinks()
    {
        var (caller, organization) = OrganizationCaller();
        _authorization.ResolveClaimedMembershipAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ClaimedMembership(TestData.Member(organization.Id, _userId, MemberRole.Viewer))));
        _plans.GetLimitsAsync(Arg.Any<enSubscriptionPlan>(), Arg.Any<CancellationToken>()).Returns(Plan(500, bulk: true));

        var result = await Sut(caller).ResolveAsync(1, false, Now, default);

        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
    }
}
