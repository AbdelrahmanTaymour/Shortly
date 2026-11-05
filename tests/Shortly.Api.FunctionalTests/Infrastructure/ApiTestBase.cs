using System.Net.Http.Json;
using System.Text.Json;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests.Infrastructure;

public abstract class ApiTestBase(ApiFixture api)
{
    protected ApiFixture Api { get; } = api;
    protected Seeder Seed => Api.Seed;

    protected HttpClient Anonymous() => Api.Default.CreateApiClient();

    protected HttpClient As(Guid userId, UserRole role = UserRole.User, Guid? organizationId = null,
        Guid? memberId = null, MemberRole? memberRole = null) =>
        Api.Default.CreateApiClient(TestTokens.Create(userId, role, organizationId, memberId, memberRole));

    /// <summary>A token that carries the organization context for an (active) member.</summary>
    protected HttpClient AsMember(Guid userId, Guid organizationId, Guid memberId, MemberRole role) =>
        As(userId, organizationId: organizationId, memberId: memberId, memberRole: role);

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>());

    /// <summary>Organization with an owner and one extra member per requested role.</summary>
    protected async Task<OrganizationFixture> CreateOrganizationAsync()
    {
        var ownerId = await Seed.CreateUserAsync();
        var organizationId = await Seed.CreateOrganizationAsync(ownerId);
        return new OrganizationFixture(this, organizationId, ownerId);
    }

    protected sealed class OrganizationFixture(ApiTestBase test, Guid organizationId, Guid ownerId)
    {
        public Guid OrganizationId { get; } = organizationId;
        public Guid OwnerId { get; } = ownerId;

        public async Task<(Guid UserId, Guid MemberId)> AddMemberAsync(MemberRole role, bool isActive = true)
        {
            var userId = await test.Seed.CreateUserAsync();
            var memberId = await test.Seed.AddMemberAsync(OrganizationId, userId, role, isActive);
            return (userId, memberId);
        }

        public HttpClient AsMember(Guid userId, Guid memberId, MemberRole role) =>
            test.AsMember(userId, OrganizationId, memberId, role);
    }
}
