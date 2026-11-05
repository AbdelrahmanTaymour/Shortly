using Microsoft.Extensions.DependencyInjection;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;

namespace Shortly.Api.FunctionalTests;

/// <summary>EF Core + PostgreSQL behaviour of the membership lookups the authorization checks depend on.</summary>
[Collection(ApiCollection.Name)]
public class MemberRepositoryTests(ApiFixture api) : ApiTestBase(api)
{
    private async Task<T> WithRepository<T>(Func<IOrganizationMemberRepository, Task<T>> action)
    {
        using var scope = Api.Default.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<IOrganizationMemberRepository>());
    }

    [Fact]
    public async Task ActiveLookup_ReturnsActiveMember()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.OrgAdmin);

        var member = await WithRepository(r => r.GetActiveByOrganizationAndUserAsync(org.OrganizationId, userId));

        member.Should().NotBeNull();
        member!.Id.Should().Be(memberId);
    }

    [Fact]
    public async Task ActiveLookup_IgnoresRemovedMember_ButUnfilteredLookupStillFindsTheRow()
    {
        var org = await CreateOrganizationAsync();
        var (userId, _) = await org.AddMemberAsync(MemberRole.OrgAdmin, isActive: false);

        var active = await WithRepository(r => r.GetActiveByOrganizationAndUserAsync(org.OrganizationId, userId));
        var any = await WithRepository(r => r.GetByOrganizationAndUserAsync(org.OrganizationId, userId));

        active.Should().BeNull();
        any.Should().NotBeNull();
        any!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ActiveLookup_IsScopedToTheOrganization()
    {
        var org = await CreateOrganizationAsync();
        var other = await CreateOrganizationAsync();

        var member = await WithRepository(r => r.GetActiveByOrganizationAndUserAsync(other.OrganizationId, org.OwnerId));

        member.Should().BeNull();
    }
}
