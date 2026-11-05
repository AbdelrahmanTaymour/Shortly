using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Cache;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.ShortUrls;
using Shortly.Core.ShortUrls.CreateShortUrl;
using Shortly.Core.ShortUrls.DeleteShortUrl;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests;

/// <summary>
/// Regression tests: JWT organization claims can outlive a membership. Operations on organization-owned
/// links must verify the CURRENT membership, not just compare the claim with the link's organization.
/// </summary>
public class ShortUrlStaleClaimTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IOrganizationAuthorizationService _organizationAuthorization =
        Substitute.For<IOrganizationAuthorizationService>();

    private void MembershipIs(bool active)
    {
        _organizationAuthorization.RequireMembershipAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns(active
                ? Result.Success(TestData.Member(_orgId, _userId, MemberRole.Member))
                : Result.Failure<OrganizationMember>(OrganizationErrors.NotAMember));
    }

    private ShortUrl OrganizationLink(Guid? organizationId = null) =>
        ShortUrl.CreateForOrganization(
            organizationId ?? _orgId, Guid.NewGuid(),
            OriginalUrl.Create("https://example.com").Value, ShortCode.Create("abc1234").Value,
            TestData.UtcNow).Value;

    private ShortUrl UserLink(Guid ownerId) =>
        ShortUrl.CreateForUser(
            ownerId, OriginalUrl.Create("https://example.com").Value, ShortCode.Create("abc1234").Value,
            TestData.UtcNow).Value;

    private Task<bool> CanManage(ShortUrl link, IUserContext caller) =>
        ShortUrlAuthorization.CanManageAsync(link, caller, _organizationAuthorization, default);

    [Fact]
    public async Task CanManage_OrganizationLink_WithMatchingClaimAndActiveMembership_IsAllowed()
    {
        MembershipIs(active: true);

        (await CanManage(OrganizationLink(), TestData.Caller(_userId, organizationId: _orgId))).Should().BeTrue();
    }

    [Theory]
    [InlineData(MemberRole.Viewer, false)]
    [InlineData(MemberRole.Member, true)]
    [InlineData(MemberRole.TeamManager, true)]
    [InlineData(MemberRole.OrgAdmin, true)]
    [InlineData(MemberRole.OrgOwner, true)]
    public async Task CanManage_OrganizationLink_RequiresAtLeastTheMemberRole(MemberRole role, bool allowed)
    {
        _organizationAuthorization.RequireMembershipAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(TestData.Member(_orgId, _userId, role)));

        (await CanManage(OrganizationLink(), TestData.Caller(_userId, organizationId: _orgId))).Should().Be(allowed);
    }

    [Fact]
    public async Task CanManage_OrganizationLink_WithMatchingClaimButRemovedMember_IsDenied()
    {
        MembershipIs(active: false);

        (await CanManage(OrganizationLink(), TestData.Caller(_userId, organizationId: _orgId))).Should().BeFalse();
    }

    [Fact]
    public async Task CanManage_OrganizationLink_WithClaimForAnotherOrganization_IsDeniedWithoutQueryingMembership()
    {
        MembershipIs(active: true);

        var allowed = await CanManage(OrganizationLink(), TestData.Caller(_userId, organizationId: Guid.NewGuid()));

        allowed.Should().BeFalse();
        await _organizationAuthorization.DidNotReceiveWithAnyArgs().RequireMembershipAsync(default, default);
    }

    [Fact]
    public async Task CanManage_OrganizationLink_WithoutOrganizationClaim_IsDenied()
    {
        MembershipIs(active: true);

        (await CanManage(OrganizationLink(), TestData.Caller(_userId))).Should().BeFalse();
    }

    [Fact]
    public async Task CanManage_UserLink_DoesNotQueryMembership()
    {
        var allowed = await CanManage(UserLink(_userId), TestData.Caller(_userId));

        allowed.Should().BeTrue();
        await _organizationAuthorization.DidNotReceiveWithAnyArgs().RequireMembershipAsync(default, default);
    }

    [Fact]
    public async Task CanManage_UserLinkOfSomeoneElse_IsDenied()
    {
        (await CanManage(UserLink(Guid.NewGuid()), TestData.Caller(_userId))).Should().BeFalse();
    }

    [Fact]
    public async Task CanManage_SystemAdmin_IsAllowedWithoutQueryingMembership()
    {
        var allowed = await CanManage(OrganizationLink(), TestData.Caller(_userId, isAdmin: true));

        allowed.Should().BeTrue();
        await _organizationAuthorization.DidNotReceiveWithAnyArgs().RequireMembershipAsync(default, default);
    }

    [Fact]
    public async Task Delete_OrganizationLink_ByRemovedMemberWithStaleClaim_IsNotFoundAndNothingDeleted()
    {
        MembershipIs(active: false);
        var link = OrganizationLink();
        var repository = Substitute.For<IShortUrlRepository>();
        repository.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(link);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new DeleteShortUrlCommandHandler(
            repository, TestData.Caller(_userId, organizationId: _orgId), _organizationAuthorization, unitOfWork);

        var result = await sut.Handle(new DeleteShortUrlCommand(1), default);

        result.Error.Should().Be(ShortUrlErrors.NotFound);
        repository.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Create_WithStaleOrganizationClaim_IsRejectedAndNothingPersisted()
    {
        _organizationAuthorization.ResolveClaimedMembershipAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ClaimedMembership>(OrganizationErrors.NotAMember));

        var caller = TestData.Caller(_userId, organizationId: _orgId);
        caller.OwnerType.Returns(enShortUrlOwnerType.Organization);

        var repository = Substitute.For<IShortUrlRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(TestData.UtcNow);
        var resolver = new ShortUrlOwnerResolver(
            caller, Substitute.For<IUserRepository>(), Substitute.For<IOrganizationRepository>(),
            _organizationAuthorization, Substitute.For<ISubscriptionPlanLimitsProvider>());
        var sut = new CreateShortUrlCommandHandler(
            repository, resolver, caller, new ShortCodeGenerator(), Substitute.For<IUnitOfWork>(), clock);

        var result = await sut.Handle(
            new CreateShortUrlCommand("https://example.com", null, null, null, null, null, null, null), default);

        result.Error.Should().Be(OrganizationErrors.NotAMember);
        repository.DidNotReceiveWithAnyArgs().Add(default!);
    }
}
