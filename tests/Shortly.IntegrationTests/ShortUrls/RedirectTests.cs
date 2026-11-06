using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.ShortUrls.RedirectToShortUrl;
using Shortly.Domain.ClickEvents;
using Shortly.Domain.ShortUrls;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.ShortUrls;

public class RedirectTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private static RedirectToShortUrlCommand Redirect(string code, string? password = null, string? campaign = null) =>
        new(code, password, "203.0.113.9", Guid.NewGuid().ToString("N"), "test-agent", null, null, null, campaign, null, null);

    private Task Sql(FormattableString sql) => Seed(db => db.Database.ExecuteSqlInterpolatedAsync(sql));

    private Task<int> Clicks(long id) => Query(db => db.Set<ShortUrl>().Where(s => s.Id == id).Select(s => s.TotalClicks).SingleAsync());

    private async Task<ShortUrl> LinkAsync(int clickLimit = -1)
    {
        var user = await Data.UserAsync();
        return await Data.UserLinkAsync(user.Id, clickLimit);
    }

    [Fact]
    public async Task Redirect_Should_ReturnTheOriginalUrl_AndCountTheClick_ForAnAnonymousVisitor()
    {
        // Arrange
        var link = await LinkAsync();

        // Act
        var result = await Send(Redirect(link.ShortCode.Value));

        // Assert
        result.Value.Should().Be(link.OriginalUrl.Value);
        (await Clicks(link.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Redirect_Should_ReturnNotFound_ForAnUnknownCode()
    {
        // Act
        var result = await Send(Redirect("zz-unknown"));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotFound);
    }

    [Fact]
    public async Task Redirect_Should_ReturnInactive_AndNotCount_ForADeactivatedLink()
    {
        // Arrange
        var link = await LinkAsync();
        await Sql($"UPDATE short_urls SET is_active = FALSE WHERE id = {link.Id}");

        // Act
        var result = await Send(Redirect(link.ShortCode.Value));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.Inactive);
        (await Clicks(link.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Redirect_Should_ReturnExpired_OnceTheExpirationPassed()
    {
        // Arrange
        var link = await LinkAsync();
        await Sql($"UPDATE short_urls SET expires_at = {TestData.Now.AddMinutes(-1)} WHERE id = {link.Id}");

        // Act
        var result = await Send(Redirect(link.ShortCode.Value));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.Expired);
        (await Clicks(link.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Redirect_Should_StillWork_BeforeTheExpiration()
    {
        // Arrange
        var link = await LinkAsync();
        await Sql($"UPDATE short_urls SET expires_at = {TestData.Now.AddHours(1)} WHERE id = {link.Id}");

        // Act
        var result = await Send(Redirect(link.ShortCode.Value));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Redirect_Should_RefuseTheClickAfterTheLimit_ButNotBefore()
    {
        // Arrange
        var link = await LinkAsync(clickLimit: 2);

        // Act
        var first = await Send(Redirect(link.ShortCode.Value));
        var second = await Send(Redirect(link.ShortCode.Value));
        var third = await Send(Redirect(link.ShortCode.Value));

        // Assert
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        third.Error.Should().Be(ShortUrlErrors.ClickLimitReached);
        (await Clicks(link.Id)).Should().Be(2);
    }

    [Fact]
    public async Task Redirect_Should_NeverLimitClicks_WhenTheLimitIsUnlimited()
    {
        // Arrange
        var link = await LinkAsync(clickLimit: -1);

        // Act
        for (var i = 0; i < 3; i++) (await Send(Redirect(link.ShortCode.Value))).IsSuccess.Should().BeTrue();

        // Assert
        (await Clicks(link.Id)).Should().Be(3);
    }

    [Fact]
    public async Task Redirect_Should_CheckThePassword_OfAProtectedLink()
    {
        // Arrange
        var link = await LinkAsync();
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash("s3cret");
            await Sql($"UPDATE short_urls SET is_password_protected = TRUE, password_hash = {hash} WHERE id = {link.Id}");
        }

        // Act
        var missing = await Send(Redirect(link.ShortCode.Value));
        var wrong = await Send(Redirect(link.ShortCode.Value, "nope"));
        var right = await Send(Redirect(link.ShortCode.Value, "s3cret"));

        // Assert
        missing.Error.Should().Be(ShortUrlErrors.PasswordRequired);
        wrong.Error.Should().Be(ShortUrlErrors.InvalidPassword);
        right.Value.Should().Be(link.OriginalUrl.Value);
        (await Clicks(link.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Redirect_Should_RecordAClickEvent_InTheBackground_WhenTrackingIsOn()
    {
        // Arrange
        var link = await LinkAsync();

        // Act
        await Send(Redirect(link.ShortCode.Value, campaign: "spring"));

        // Assert: the click is queued and written by the background service
        var stored = await Eventually(async () =>
            await Query(db => db.Set<ClickEvent>().Where(c => c.ShortUrlId == link.Id).ToListAsync()), clicks => clicks.Count == 1);
        stored.Should().ContainSingle().Which.UtmCampaign.Should().Be("spring");
    }

    [Fact]
    public async Task Redirect_Should_NotRecordAClickEvent_WhenTrackingIsOff()
    {
        // Arrange
        var link = await LinkAsync();
        await Sql($"UPDATE short_urls SET tracking_enabled = FALSE WHERE id = {link.Id}");

        // Act
        await Send(Redirect(link.ShortCode.Value));
        await Task.Delay(1500);

        // Assert
        (await Clicks(link.Id)).Should().Be(1);
        (await Query(db => db.Set<ClickEvent>().CountAsync(c => c.ShortUrlId == link.Id))).Should().Be(0);
    }

    [Fact]
    public async Task Redirect_Should_NotLetConcurrentVisitorsExceedTheClickLimit()
    {
        // Arrange
        var link = await LinkAsync(clickLimit: 1);

        // Act: forty visitors at the same moment on a link that allows one click
        var results = await Task.WhenAll(Enumerable.Range(0, 40).Select(_ => Send(Redirect(link.ShortCode.Value))));

        // Assert
        results.Count(r => r.IsSuccess).Should().Be(1);
        (await Clicks(link.Id)).Should().Be(1);
    }

    private static async Task<T> Eventually<T>(Func<Task<T>> read, Func<T, bool> done)
    {
        var value = await read();
        for (var i = 0; i < 60 && !done(value); i++)
        {
            await Task.Delay(250);
            value = await read();
        }
        return value;
    }
}
