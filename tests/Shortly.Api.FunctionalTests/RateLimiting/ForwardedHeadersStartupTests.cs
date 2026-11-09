using Shortly.Api.FunctionalTests.Infrastructure;

namespace Shortly.Api.FunctionalTests.RateLimiting;

[Collection(ApiCollection.Name)]
public class ForwardedHeadersStartupTests(ApiFixture api)
{
    [Fact]
    public void ApplicationRefusesToStart_WhenTrustAllProxiesIsCombinedWithAKnownProxy()
    {
        var act = () => api.CreateFactory(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:TrustAllProxies"] = "true",
            ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5"
        });

        act.Should().Throw<Exception>().Which.ToString().Should().Contain("TrustAllProxies");
    }
}
