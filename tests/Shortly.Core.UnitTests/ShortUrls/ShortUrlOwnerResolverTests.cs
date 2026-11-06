using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Cache;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.ShortUrls;

/// <summary>The rules single and bulk creation share: owner status, plan entitlement and monthly quota.</summary>
public class ShortUrlOwnerResolverTests
{
    private static readonly DateTime Now = TestClock.UtcNow;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly OwnerResolverData _data = new();

    private static SubscriptionPlanLimits Plan(int maxLinks, bool bulk) => PlanLimitsData.With(maxLinks, bulk);

    private IUserContext PersonalCaller()
    {
        var context = CallerData.Create(_userId);
        context.OwnerType.Returns(enShortUrlOwnerType.User);
        return context;
    }

    private static IUserContext AnonymousCaller(string? sessionId = "session")
    {
        var context = Substitute.For<IUserContext>();
        context.OwnerType.Returns(enShortUrlOwnerType.Anonymous);
        context.AnonymousSessionId.Returns(sessionId);
        return context;
    }

    private User UserWithUsage(int linksThisMonth, enSubscriptionPlan plan = enSubscriptionPlan.Free)
    {
        var user = UserData.Create(email: $"{Guid.NewGuid():N}@example.com");
        user.ChangeSubscriptionPlan(plan, Now);
        for (var i = 0; i < linksThisMonth; i++) user.Usage.IncrementLinksCreated(Now);
        _data.Users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private (IUserContext Caller, Organization Organization) OrganizationCaller(
        MemberRole role = MemberRole.Member, bool active = true, int linksThisMonth = 0)
    {
        var organization = OrganizationData.Create(plan: enSubscriptionPlan.Professional);
        for (var i = 0; i < linksThisMonth; i++) organization.Usage.IncrementLinksCreated(Now);
        if (!active) organization.Deactivate(Now);

        var context = CallerData.Create(_userId, organizationId: organization.Id);
        context.OwnerType.Returns(enShortUrlOwnerType.Organization);

        _data.Authorization.ResolveClaimedMembershipAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ClaimedMembership(MemberData.Create(organization.Id, _userId, role))));
        _data.Organizations.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);
        return (context, organization);
    }

    // ---- personal owner ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task ResolveAsync_Should_ReportTheRemainingQuota_ForAPersonalOwner()
    {
        // Arrange
        UserWithUsage(linksThisMonth: 3);
        _data.WithLimits(Plan(5, bulk: false));

        // Act
        var result = await _data.Create(PersonalCaller()).ResolveAsync(null, false, Now, default);

        // Assert
        result.Value.RemainingMonthlyLinks.Should().Be(2);
    }

    [Fact]
    public async Task ResolveAsync_Should_ReturnNotFound_When_ThePersonalOwnerNoLongerExists()
    {
        // Arrange
        _data.Users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _data.Create(PersonalCaller()).ResolveAsync(null, false, Now, default);

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task ResolveAsync_Should_ReturnMonthlyLimitReached_When_TheRequestedLinksExceedTheQuota()
    {
        // Arrange
        UserWithUsage(linksThisMonth: 4);
        _data.WithLimits(Plan(5, bulk: false));

        // Act
        var result = await _data.Create(PersonalCaller()).ResolveAsync(2, false, Now, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.MonthlyLinkLimitReached);
    }

    [Fact]
    public async Task ResolveAsync_Should_Succeed_When_TheRequestedLinksExactlyFillTheQuota()
    {
        // Arrange
        UserWithUsage(linksThisMonth: 4);
        _data.WithLimits(Plan(5, bulk: false));

        // Act
        var result = await _data.Create(PersonalCaller()).ResolveAsync(1, false, Now, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_Should_ReportAnUnboundedQuota_OnAnUnlimitedPlan()
    {
        // Arrange
        UserWithUsage(linksThisMonth: 100_000);
        _data.WithLimits(Plan(-1, bulk: true));

        // Act
        var result = await _data.Create(PersonalCaller()).ResolveAsync(500, true, Now, default);

        // Assert
        result.Value.RemainingMonthlyLinks.Should().Be(int.MaxValue);
    }

    [Fact]
    public async Task ResolveAsync_Should_NotCountLastMonthsLinks_AgainstTheNewMonth()
    {
        // Arrange: the counter resets one month after registration
        UserWithUsage(linksThisMonth: 5);
        _data.WithLimits(Plan(5, bulk: false));

        // Act
        var result = await _data.Create(PersonalCaller()).ResolveAsync(null, false, Now.AddMonths(2), default);

        // Assert
        result.Value.RemainingMonthlyLinks.Should().Be(5);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task ResolveAsync_Should_RequireTheBulkEntitlement_ForBulkCreation(bool planAllowsBulk, bool expectedSuccess)
    {
        // Arrange
        UserWithUsage(linksThisMonth: 0);
        _data.WithLimits(Plan(500, planAllowsBulk));

        // Act
        var result = await _data.Create(PersonalCaller()).ResolveAsync(null, requireBulkCreation: true, Now, default);

        // Assert
        result.IsSuccess.Should().Be(expectedSuccess);
        if (!expectedSuccess) result.Error.Should().Be(ShortUrlErrors.BulkCreationNotAllowed);
    }

    [Fact]
    public async Task ResolveAsync_Should_NotRequireTheBulkEntitlement_ForSingleCreation()
    {
        // Arrange
        UserWithUsage(linksThisMonth: 0);
        _data.WithLimits(Plan(5, bulk: false));

        // Act
        var result = await _data.Create(PersonalCaller()).ResolveAsync(1, requireBulkCreation: false, Now, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_Should_CarryTheOwnersLimits_NotAnotherPlans()
    {
        // Arrange
        var professional = PlanLimitsData.With(protection: true, customCode: true);
        UserWithUsage(linksThisMonth: 0, plan: enSubscriptionPlan.Professional);
        _data.Plans.GetLimitsAsync(enSubscriptionPlan.Professional, Arg.Any<CancellationToken>()).Returns(professional);

        // Act
        var result = await _data.Create(PersonalCaller()).ResolveAsync(null, false, Now, default);

        // Assert
        result.Value.Limits.Should().BeSameAs(professional);
    }

    [Fact]
    public async Task ResolveAsync_Should_ReturnInvalidOwnerType_ForAnUnknownOwnerType()
    {
        // Arrange
        var caller = CallerData.Create(_userId);
        caller.OwnerType.Returns((enShortUrlOwnerType)99);

        // Act
        var result = await _data.Create(caller).ResolveAsync(null, false, Now, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidOwnerType);
    }

    // ---- anonymous owner ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task ResolveAsync_Should_GiveAnonymousCallersTheFreePlansEntitlements()
    {
        // Arrange
        var free = PlanLimitsData.With();
        _data.Plans.GetLimitsAsync(enSubscriptionPlan.Free, Arg.Any<CancellationToken>()).Returns(free);

        // Act
        var result = await _data.Create(AnonymousCaller()).ResolveAsync(1, false, Now, default);

        // Assert
        result.Value.Limits.Should().BeSameAs(free);
        result.Value.AnonymousSessionId.Should().Be("session");
        await _data.Plans.DidNotReceive().GetLimitsAsync(
            Arg.Is<enSubscriptionPlan>(p => p != enSubscriptionPlan.Free), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveAsync_Should_NotLimitTheMonthlyQuota_OfAnonymousCallers()
    {
        // Arrange
        _data.WithLimits(PlanLimitsData.With(maxLinks: 5));

        // Act
        var result = await _data.Create(AnonymousCaller()).ResolveAsync(1000, false, Now, default);

        // Assert: anonymous creation is limited by rate limiting, not by a quota
        result.Value.RemainingMonthlyLinks.Should().Be(int.MaxValue);
    }

    [Fact]
    public async Task ResolveAsync_Should_RefuseBulkCreation_ForAnonymousCallers()
    {
        // Act
        var result = await _data.Create(AnonymousCaller()).ResolveAsync(null, requireBulkCreation: true, Now, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.BulkCreationNotAllowed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ResolveAsync_Should_ReturnInvalidOwnerType_When_AnAnonymousCallerHasNoSession(string? sessionId)
    {
        // Act
        var result = await _data.Create(AnonymousCaller(sessionId)).ResolveAsync(null, false, Now, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidOwnerType);
    }

    // ---- organization owner ------------------------------------------------------------------------------------------

    [Fact]
    public async Task ResolveAsync_Should_ReturnInactive_When_TheOrganizationIsInactive()
    {
        // Arrange
        var (caller, _) = OrganizationCaller(active: false);
        _data.WithLimits(Plan(500, bulk: true));

        // Act
        var result = await _data.Create(caller).ResolveAsync(null, true, Now, default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.Inactive);
    }

    [Fact]
    public async Task ResolveAsync_Should_ReturnNotFound_When_TheOrganizationNoLongerExists()
    {
        // Arrange
        var (caller, organization) = OrganizationCaller();
        _data.Organizations.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns((Organization?)null);

        // Act
        var result = await _data.Create(caller).ResolveAsync(null, false, Now, default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
    }

    [Fact]
    public async Task ResolveAsync_Should_UseTheDatabaseMemberId_AndReportTheOrganizationsQuota()
    {
        // Arrange
        var (caller, organization) = OrganizationCaller(linksThisMonth: 100);
        _data.WithLimits(Plan(500, bulk: true));

        // Act
        var result = await _data.Create(caller).ResolveAsync(null, true, Now, default);

        // Assert
        result.Value.OrganizationId.Should().Be(organization.Id);
        result.Value.MemberId.Should().NotBeNull();
        result.Value.RemainingMonthlyLinks.Should().Be(400);
    }

    [Fact]
    public async Task ResolveAsync_Should_ReturnNotAMember_When_TheOrganizationClaimIsStale()
    {
        // Arrange
        var (caller, _) = OrganizationCaller();
        _data.Authorization.ResolveClaimedMembershipAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ClaimedMembership>(OrganizationErrors.NotAMember));

        // Act
        var result = await _data.Create(caller).ResolveAsync(null, true, Now, default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task ResolveAsync_Should_ReturnInvalidOwnerType_When_NoOrganizationIsClaimed()
    {
        // Arrange: the token says "organization workspace" but resolves to the personal workspace
        var (caller, _) = OrganizationCaller();
        _data.Authorization.ResolveClaimedMembershipAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success(ClaimedMembership.Personal));

        // Act
        var result = await _data.Create(caller).ResolveAsync(null, false, Now, default);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidOwnerType);
    }

    [Fact]
    public async Task ResolveAsync_Should_ReturnInsufficientRole_When_TheCallerIsAViewer()
    {
        // Arrange
        var (caller, _) = OrganizationCaller(role: MemberRole.Viewer);
        _data.WithLimits(Plan(500, bulk: true));

        // Act
        var result = await _data.Create(caller).ResolveAsync(1, false, Now, default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
    }

    [Fact]
    public async Task RecordLinksCreated_Should_IncreaseTheOrganizationsUsageByTheCount()
    {
        // Arrange
        var (caller, organization) = OrganizationCaller(linksThisMonth: 10);
        _data.WithLimits(Plan(500, bulk: true));
        var owner = (await _data.Create(caller).ResolveAsync(null, true, Now, default)).Value;

        // Act
        owner.RecordLinksCreated(7, Now);

        // Assert
        organization.Usage.MonthlyLinksCreated.Should().Be(17);
    }

    [Fact]
    public async Task RecordLinksCreated_Should_IncreaseThePersonalUsageByTheCount()
    {
        // Arrange
        var user = UserWithUsage(linksThisMonth: 1);
        _data.WithLimits(Plan(500, bulk: true));
        var owner = (await _data.Create(PersonalCaller()).ResolveAsync(null, false, Now, default)).Value;

        // Act
        owner.RecordLinksCreated(3, Now);

        // Assert
        user.Usage.MonthlyLinksCreated.Should().Be(4);
    }

    // ---- which plan applies to an existing link ----------------------------------------------------------------------

    [Fact]
    public async Task GetLimitsForLinkAsync_Should_UseTheOwningUsersPlan_NotTheCallers()
    {
        // Arrange
        var owner = UserData.Create(email: "owner@example.com", username: "owner");
        owner.ChangeSubscriptionPlan(enSubscriptionPlan.Starter, Now);
        _data.Users.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);
        var starter = PlanLimitsData.With(customCode: true);
        _data.Plans.GetLimitsAsync(enSubscriptionPlan.Starter, Arg.Any<CancellationToken>()).Returns(starter);

        // Act
        var limits = await _data.Create(PersonalCaller()).GetLimitsForLinkAsync(ShortUrlData.UserLink(owner.Id), default);

        // Assert
        limits.Should().BeSameAs(starter);
        await _data.Users.DidNotReceive().GetByIdAsync(_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLimitsForLinkAsync_Should_UseTheOrganizationsPlan_ForAnOrganizationLink()
    {
        // Arrange
        var organization = OrganizationData.Create(plan: enSubscriptionPlan.Professional);
        _data.Organizations.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);
        var professional = PlanLimitsData.With(protection: true, customCode: true);
        _data.Plans.GetLimitsAsync(enSubscriptionPlan.Professional, Arg.Any<CancellationToken>()).Returns(professional);

        // Act
        var limits = await _data.Create(PersonalCaller()).GetLimitsForLinkAsync(ShortUrlData.OrganizationLink(organization.Id), default);

        // Assert
        limits.Should().BeSameAs(professional);
    }

    [Fact]
    public async Task GetLimitsForLinkAsync_Should_UseTheFreePlan_ForAnAnonymousLink()
    {
        // Arrange
        var free = PlanLimitsData.With();
        _data.Plans.GetLimitsAsync(enSubscriptionPlan.Free, Arg.Any<CancellationToken>()).Returns(free);
        var anonymousLink = ShortUrl.CreateForAnonymous(
            "session", "203.0.113.1", OriginalUrl.Create("https://example.com").Value, ShortCode.Create("anon123").Value, Now).Value;

        // Act
        var limits = await _data.Create(PersonalCaller()).GetLimitsForLinkAsync(anonymousLink, default);

        // Assert
        limits.Should().BeSameAs(free);
    }

    [Fact]
    public async Task GetLimitsForLinkAsync_Should_UseTheFreePlan_When_TheOwningUserNoLongerExists()
    {
        // Arrange
        var free = PlanLimitsData.With();
        _data.Plans.GetLimitsAsync(enSubscriptionPlan.Free, Arg.Any<CancellationToken>()).Returns(free);
        _data.Users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var limits = await _data.Create(PersonalCaller()).GetLimitsForLinkAsync(ShortUrlData.UserLink(Guid.NewGuid()), default);

        // Assert
        limits.Should().BeSameAs(free);
    }

    [Fact]
    public async Task GetLimitsForLinkAsync_Should_UseTheFreePlan_When_TheOwningOrganizationNoLongerExists()
    {
        // Arrange
        var free = PlanLimitsData.With();
        _data.Plans.GetLimitsAsync(enSubscriptionPlan.Free, Arg.Any<CancellationToken>()).Returns(free);
        _data.Organizations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Organization?)null);

        // Act
        var limits = await _data.Create(PersonalCaller()).GetLimitsForLinkAsync(ShortUrlData.OrganizationLink(Guid.NewGuid()), default);

        // Assert
        limits.Should().BeSameAs(free);
    }
}
