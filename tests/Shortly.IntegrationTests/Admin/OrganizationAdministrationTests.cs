using Microsoft.EntityFrameworkCore;
using Shortly.Core.Admin.ChangeOrganizationPlan;
using Shortly.Core.Admin.GetOrganizationDetail;
using Shortly.Core.Admin.SearchOrganizations;
using Shortly.Core.Exceptions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.SubscriptionPlan;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Admin;

public class OrganizationAdministrationTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task<Organization> Stored(Guid id) => Query(db => db.Set<Organization>().Include(o => o.Usage).SingleAsync(o => o.Id == id));
 
    // ---- GetOrganizationDetail ---------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task GetDetail_Should_ReturnTheOwner_EveryMember_AndEveryTeam_WithItsMemberCount()
    {
        // Arrange
        var org = await Data.OrganizationAsync(name: "Acme Inc");
        var (adminUser, adminMember) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        var (removedUser, _) = await Data.MemberAsync(org.Id, isActive: false);
        var team = await Data.TeamAsync(org.Id, adminMember.Id, "Growth");
        var (extraUser, extraMember) = await Data.MemberAsync(org.Id);
        await Data.TeamMemberAsync(team.Id, extraMember.Id);
        As(Caller.Admin());
 
        // Act
        var result = await Send(new GetOrganizationDetailQuery(org.Id));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        var detail = result.Value;
        detail.Name.Should().Be("Acme Inc");
        detail.OwnerId.Should().Be(org.Owner.Id);
        detail.OwnerEmail.Should().Be(org.Owner.Email);
        detail.SubscriptionPlan.Should().Be(enSubscriptionPlan.Free);
        detail.Members.Select(m => m.UserId).Should().BeEquivalentTo([org.Owner.Id, adminUser.Id, removedUser.Id, extraUser.Id]);
        detail.Members.Single(m => m.UserId == adminUser.Id).Role.Should().Be(MemberRole.OrgAdmin);
        detail.Members.Single(m => m.UserId == removedUser.Id).IsActive.Should().BeFalse();
        detail.Teams.Should().ContainSingle(t => t.Id == team.Id && t.Name == "Growth" && t.MemberCount == 2);
    }
 
    [Fact]
    public async Task GetDetail_Should_ReturnNotFound_ForAnUnknownOrganization()
    {
        // Arrange
        As(Caller.Admin());
 
        // Act
        var result = await Send(new GetOrganizationDetailQuery(Guid.NewGuid()));
 
        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
    }
 
    [Fact]
    public async Task GetDetail_Should_ReturnNotFound_ForASoftDeletedOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        await Seed(async db => (await db.Set<Organization>().SingleAsync(o => o.Id == org.Id)).SoftDelete(TestData.Now));
        As(Caller.Admin());
 
        // Act
        var result = await Send(new GetOrganizationDetailQuery(org.Id));
 
        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
    }
 
    // ---- SearchOrganizations ------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task Search_Should_FilterByName_CaseInsensitively_AndExcludeSoftDeletedOrganizations()
    {
        // Arrange
        var match = await Data.OrganizationAsync(name: $"Acme Rockets {Guid.NewGuid():N}");
        var other = await Data.OrganizationAsync(name: $"Globex {Guid.NewGuid():N}");
        var deletedMatch = await Data.OrganizationAsync(name: $"Acme Deleted {Guid.NewGuid():N}");
        await Seed(async db => (await db.Set<Organization>().SingleAsync(o => o.Id == deletedMatch.Id)).SoftDelete(TestData.Now));
        As(Caller.Admin());
 
        // Act
        var result = await Send(new SearchOrganizationsQuery("acme rockets"));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(o => o.Id).Should().ContainSingle().Which.Should().Be(match.Id);
        result.Value.Items.Select(o => o.Id).Should().NotContain([other.Id, deletedMatch.Id]);
    }
 
    [Fact]
    public async Task Search_Should_PageResults_NewestFirst_AndReportAccurateTotals()
    {
        // Arrange: three organizations created in order, one page at a time
        var tag = Guid.NewGuid().ToString("N");
        var first = await Data.OrganizationAsync(name: $"Paging {tag} 1");
        var second = await Data.OrganizationAsync(name: $"Paging {tag} 2");
        var third = await Data.OrganizationAsync(name: $"Paging {tag} 3");
        As(Caller.Admin());
 
        // Act
        var page1 = (await Send(new SearchOrganizationsQuery(tag, Page: 1, PageSize: 2))).Value;
        var page2 = (await Send(new SearchOrganizationsQuery(tag, Page: 2, PageSize: 2))).Value;
 
        // Assert
        page1.TotalCount.Should().Be(3);
        page1.TotalPages.Should().Be(2);
        page1.Items.Select(o => o.Id).Should().Equal(third.Id, second.Id);
        page2.Items.Select(o => o.Id).Should().Equal(first.Id);
    }
 
    [Fact]
    public async Task Search_Should_ReturnEveryActiveOrganization_WhenNoSearchTermIsGiven()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        As(Caller.Admin());
 
        // Act
        var result = await Send(new SearchOrganizationsQuery(null));
 
        // Assert
        result.Value.Items.Select(o => o.Id).Should().Contain(org.Id);
    }
 
    // ---- ChangeOrganizationPlan ----------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task ChangePlan_Should_UpdateThePlan_AndMoveTheMemberLimit_ToMatchTheNewPlan()
    {
        // Arrange: Free starts with a member limit of 3
        var org = await Data.OrganizationAsync();
        As(Caller.Admin());
 
        // Act
        var result = await Send(new ChangeOrganizationPlanCommand(org.Id, enSubscriptionPlan.Starter));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await Stored(org.Id);
        stored.SubscriptionPlanId.Should().Be(enSubscriptionPlan.Starter);
        stored.IsSubscribed.Should().BeTrue();
        stored.MemberLimit.Should().Be(10);
    }
 
    [Fact]
    public async Task ChangePlan_Should_SetAnEffectivelyUnlimitedMemberLimit_ForEnterprise()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        As(Caller.Admin());
 
        // Act
        var result = await Send(new ChangeOrganizationPlanCommand(org.Id, enSubscriptionPlan.Enterprise));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await Stored(org.Id);
        stored.MemberLimit.Should().Be(int.MaxValue);
    }
 
    [Fact]
    public async Task ChangePlan_Should_MarkTheOrganizationAsUnsubscribed_WhenMovedBackToFree()
    {
        // Arrange
        var org = await Data.OrganizationAsync(plan: enSubscriptionPlan.Professional, memberLimit: 50);
        As(Caller.Admin());
 
        // Act
        var result = await Send(new ChangeOrganizationPlanCommand(org.Id, enSubscriptionPlan.Free));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await Stored(org.Id);
        stored.IsSubscribed.Should().BeFalse();
        stored.MemberLimit.Should().Be(3);
    }
 
    [Fact]
    public async Task ChangePlan_Should_ReturnNotFound_ForAnUnknownOrganization()
    {
        // Arrange
        As(Caller.Admin());
 
        // Act
        var result = await Send(new ChangeOrganizationPlanCommand(Guid.NewGuid(), enSubscriptionPlan.Starter));
 
        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
    }
 
    [Fact]
    public async Task ChangePlan_Should_RejectAnUndefinedSubscriptionPlan_BeforeTouchingTheOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        As(Caller.Admin());
 
        // Act
        var act = () => Send(new ChangeOrganizationPlanCommand(org.Id, (enSubscriptionPlan)99));
 
        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        (await Stored(org.Id)).SubscriptionPlanId.Should().Be(enSubscriptionPlan.Free);
    }
}
