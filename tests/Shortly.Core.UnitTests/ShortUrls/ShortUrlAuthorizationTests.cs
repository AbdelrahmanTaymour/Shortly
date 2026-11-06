using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.ShortUrls;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.UnitTests.ShortUrls;

/// <summary>
/// JWT organization claims can outlive a membership, so managing an organization link must verify the CURRENT membership,
/// not just compare the claim with the link's organization.
/// </summary>
public class ShortUrlAuthorizationTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IOrganizationAuthorizationService _organizationAuthorization =
        Substitute.For<IOrganizationAuthorizationService>();

    private void MembershipIs(bool active) =>
        _organizationAuthorization.RequireMembershipAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns(active
                ? Result.Success(MemberData.Create(_orgId, _userId, MemberRole.Member))
                : Result.Failure<OrganizationMember>(OrganizationErrors.NotAMember));

    private Task<bool> CanManage(ShortUrl link, IUserContext caller) =>
        ShortUrlAuthorization.CanManageAsync(link, caller, _organizationAuthorization, default);

    [Fact]
    public async Task CanManageAsync_Should_BeTrue_When_ForAnOrganizationLinkTheClaimMatchesAndTheMembershipIsActive()
    {
        // Arrange
        MembershipIs(active: true);

        // Act
        var allowed = await CanManage(ShortUrlData.OrganizationLink(_orgId), CallerData.Create(_userId, organizationId: _orgId));

        // Assert
        allowed.Should().BeTrue();
    }

    [Theory]
    [InlineData(MemberRole.Viewer, false)]
    [InlineData(MemberRole.Member, true)]
    [InlineData(MemberRole.TeamManager, true)]
    [InlineData(MemberRole.OrgAdmin, true)]
    [InlineData(MemberRole.OrgOwner, true)]
    public async Task CanManageAsync_Should_RequireAtLeastTheMemberRole_ForAnOrganizationLink(MemberRole role, bool expected)
    {
        // Arrange
        _organizationAuthorization.RequireMembershipAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(MemberData.Create(_orgId, _userId, role)));

        // Act
        var allowed = await CanManage(ShortUrlData.OrganizationLink(_orgId), CallerData.Create(_userId, organizationId: _orgId));

        // Assert
        allowed.Should().Be(expected);
    }

    [Fact]
    public async Task CanManageAsync_Should_BeFalse_When_TheClaimMatchesButTheMemberWasRemoved()
    {
        // Arrange
        MembershipIs(active: false);

        // Act
        var allowed = await CanManage(ShortUrlData.OrganizationLink(_orgId), CallerData.Create(_userId, organizationId: _orgId));

        // Assert
        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task CanManageAsync_Should_BeFalse_WithoutQueryingMembership_When_TheClaimIsForAnotherOrganization()
    {
        // Arrange
        MembershipIs(active: true);

        // Act
        var allowed = await CanManage(ShortUrlData.OrganizationLink(_orgId), CallerData.Create(_userId, organizationId: Guid.NewGuid()));

        // Assert
        allowed.Should().BeFalse();
        await _organizationAuthorization.DidNotReceiveWithAnyArgs().RequireMembershipAsync(default, default);
    }

    [Fact]
    public async Task CanManageAsync_Should_BeFalse_When_ThereIsNoOrganizationClaim()
    {
        // Arrange
        MembershipIs(active: true);

        // Act
        var allowed = await CanManage(ShortUrlData.OrganizationLink(_orgId), CallerData.Create(_userId));

        // Assert
        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task CanManageAsync_Should_BeTrue_WithoutQueryingMembership_ForTheOwnerOfAUserLink()
    {
        // Act
        var allowed = await CanManage(ShortUrlData.UserLink(_userId), CallerData.Create(_userId));

        // Assert
        allowed.Should().BeTrue();
        await _organizationAuthorization.DidNotReceiveWithAnyArgs().RequireMembershipAsync(default, default);
    }

    [Fact]
    public async Task CanManageAsync_Should_BeFalse_ForSomeoneElsesUserLink()
    {
        // Act
        var allowed = await CanManage(ShortUrlData.UserLink(Guid.NewGuid()), CallerData.Create(_userId));

        // Assert
        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task CanManageAsync_Should_BeTrue_WithoutQueryingMembership_ForASystemAdmin()
    {
        // Act
        var allowed = await CanManage(ShortUrlData.OrganizationLink(_orgId), CallerData.Create(_userId, isAdmin: true));

        // Assert
        allowed.Should().BeTrue();
        await _organizationAuthorization.DidNotReceiveWithAnyArgs().RequireMembershipAsync(default, default);
    }
}
