using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests;

/// <summary>
/// A request that fails validation inside the MediatR pipeline is the caller's error (400 with the validation errors),
/// never a 500; and a path that cannot be a short code is a 404.
/// </summary>
[Collection(ApiCollection.Name)]
public class ValidationResponseTests(ApiFixture api) : ApiTestBase(api)
{
    [Fact]
    public async Task PasswordLongerThanTheLimit_OnVerify_Returns400WithTheValidationErrors()
    {
        var owner = await Seed.CreateUserAsync();
        await Seed.SetUserPlanAsync(owner, enSubscriptionPlan.Professional);
        var linkId = await Seed.CreateUserLinkAsync(owner);
        await As(owner).PostAsJsonAsync($"/api/short-urls/{linkId}/password", new { password = "a-valid-password-1" });
        var link = (await Seed.GetLinkAsync(linkId))!;
        var client = Api.CreateFactory().CreateApiClient();
        var page = await client.GetAsync($"/{link.ShortCode}");
        var token = HttpUtility.ParseQueryString(new Uri(new Uri("https://localhost"), page.Headers.Location!).Query)["token"]!;

        var response = await client.PostAsync("/api/url-redirect/verify",
            new FormUrlEncodedContent([new("token", token), new("password", new string('p', 129))]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("errors").EnumerateObject().Select(e => e.Name).Should().Contain("Password");
    }

    [Fact]
    public async Task PathLongerThanAnyShortCode_Returns404_NotAServerError()
    {
        (await Anonymous().GetAsync($"/{new string('a', 51)}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Anonymous().GetAsync($"/{new string('a', 5000)}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ForceUpdateUser_WithAnUndefinedPlan_Returns400AndChangesNothing()
    {
        var user = await Seed.CreateUserAsync();
        var admin = As(Guid.NewGuid(), UserRole.SystemAdmin);

        var response = await admin.PatchAsJsonAsync($"/api/admin/users/{user}", new { subscriptionPlanId = 99 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Seed.GetUserPlanAsync(user)).Should().Be(enSubscriptionPlan.Free);
    }

    [Fact]
    public async Task ForceUpdateUser_WithAValidPlan_StillWorks()
    {
        var user = await Seed.CreateUserAsync();

        var response = await As(Guid.NewGuid(), UserRole.SystemAdmin)
            .PatchAsJsonAsync($"/api/admin/users/{user}", new { subscriptionPlanId = (int)enSubscriptionPlan.Professional });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Seed.GetUserPlanAsync(user)).Should().Be(enSubscriptionPlan.Professional);
    }

    [Fact]
    public async Task ValidationErrors_RaisedByTheMvcPipeline_AreStill400()
    {
        // Existing behavior preserved: the request-model validator path is unchanged.
        var user = await Seed.CreateUserAsync();
        await Seed.SetUserPlanAsync(user, enSubscriptionPlan.Professional);

        var response = await As(user).PostAsJsonAsync("/api/short-urls/bulk", new { urls = new[] { new { originalUrl = "not a url" } } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
