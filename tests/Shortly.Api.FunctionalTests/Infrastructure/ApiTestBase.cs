using System.Net.Http.Json;
using System.Text.Json;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests.Infrastructure;

/// <summary>
/// Serves as the base class for integration tests within the "api" collection, providing 
/// factory helpers for authenticated clients, role-based scoping, and domain fixture management.
/// </summary>
public abstract class ApiTestBase(ApiFixture api)
{
    protected ApiFixture Api { get; } = api;
    protected Seeder Seed => Api.Seed;

    /// <summary>Creates an unauthenticated HTTP client for testing anonymous endpoints.</summary>
    protected HttpClient Anonymous() => Api.Default.CreateApiClient();

    /// <summary>Creates an HTTP client authenticated as a specific user with optional role and tenancy context.</summary>
    protected HttpClient As(Guid userId, UserRole role = UserRole.User, Guid? organizationId = null,
        Guid? memberId = null, MemberRole? memberRole = null) =>
        Api.Default.CreateApiClient(TestTokens.Create(userId, role, organizationId, memberId, memberRole));

    /// <summary>Creates an HTTP client carrying the required organization membership context.</summary>
    protected HttpClient AsMember(Guid userId, Guid organizationId, Guid memberId, MemberRole role) =>
        As(userId, organizationId: organizationId, memberId: memberId, memberRole: role);

    /// <summary>Reads and parses the HTTP response content as a JSON element.</summary>
    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>());

    /// <summary>Creates a test organization with an owner pre-seeded in the database.</summary>
    protected async Task<OrganizationFixture> CreateOrganizationAsync()
    {
        var ownerId = await Seed.CreateUserAsync();
        var organizationId = await Seed.CreateOrganizationAsync(ownerId);
        return new OrganizationFixture(this, organizationId, ownerId);
    }

    /// <summary>Encapsulates an organization and its associated members for test scenarios.</summary>
    protected sealed class OrganizationFixture(ApiTestBase test, Guid organizationId, Guid ownerId)
    {
        public Guid OrganizationId { get; } = organizationId;
        public Guid OwnerId { get; } = ownerId;

        /// <summary>Adds a new member with the specified role to the test organization.</summary>
        public async Task<(Guid UserId, Guid MemberId)> AddMemberAsync(MemberRole role, bool isActive = true)
        {
            var userId = await test.Seed.CreateUserAsync();
            var memberId = await test.Seed.AddMemberAsync(OrganizationId, userId, role, isActive);
            return (userId, memberId);
        }

        /// <summary>Creates an HTTP client authenticated as a member of this specific organization.</summary>
        public HttpClient AsMember(Guid userId, Guid memberId, MemberRole role) =>
            test.AsMember(userId, OrganizationId, memberId, role);
    }
}