using System.Net;
using Shortly.Api.FunctionalTests.Infrastructure;

namespace Shortly.Api.FunctionalTests.ShortUrls;

[Collection(ApiCollection.Name)]
public class NumericShortCodeTests(ApiFixture api) : ApiTestBase(api)
{
    private static string DigitCode() => (Random.Shared.NextInt64(10_000_000, 99_999_999)).ToString();

    [Fact]
    public async Task NumericShortCode_CanBeFetchedByCode_ByItsOwner()
    {
        var owner = await Seed.CreateUserAsync();
        var code = DigitCode();
        var linkId = await Seed.CreateUserLinkAsync(owner, shortCode: code);

        var response = await As(owner).GetAsync($"/api/short-urls/{code}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        json.GetProperty("shortCode").GetString().Should().Be(code);
        json.GetProperty("id").GetInt64().Should().Be(linkId);
    }

    [Fact]
    public async Task NumericShortCode_WithLeadingZeros_KeepsTheExactText()
    {
        var owner = await Seed.CreateUserAsync();
        var code = "00" + DigitCode();
        await Seed.CreateUserLinkAsync(owner, shortCode: code);

        var response = await As(owner).GetAsync($"/api/short-urls/{code}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(response)).GetProperty("shortCode").GetString().Should().Be(code);
    }

    [Fact]
    public async Task NumericShortCode_IsNotVisibleToAnotherUser()
    {
        var owner = await Seed.CreateUserAsync();
        var code = DigitCode();
        await Seed.CreateUserLinkAsync(owner, shortCode: code);

        (await As(await Seed.CreateUserAsync()).GetAsync($"/api/short-urls/{code}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LookupByRealId_StillWins_AndUnknownNumbersStay404()
    {
        var owner = await Seed.CreateUserAsync();
        var linkId = await Seed.CreateUserLinkAsync(owner);
        var client = As(owner);

        (await ReadJsonAsync(await client.GetAsync($"/api/short-urls/{linkId}"))).GetProperty("id").GetInt64().Should().Be(linkId);
        (await client.GetAsync("/api/short-urls/987654321098")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}