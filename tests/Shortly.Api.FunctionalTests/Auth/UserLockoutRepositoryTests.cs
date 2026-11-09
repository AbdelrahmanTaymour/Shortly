using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Users;
using Shortly.Infrastructure;

namespace Shortly.Api.FunctionalTests.Auth;

[Collection(ApiCollection.Name)]
public class UserLockoutRepositoryTests(ApiFixture api) : ApiTestBase(api)
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan Lockout = TimeSpan.FromMinutes(15);
    private static readonly DateTime Now = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private async Task RecordFailure(Guid userId, DateTime utcNow, int maxAttempts = MaxAttempts)
    {
        using var scope = Api.Default.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IUserRepository>()
            .RecordFailedLoginAsync(userId, utcNow, maxAttempts, Lockout);
    }

    private async Task<UserSecurity> SecurityOf(Guid userId)
    {
        using var scope = Api.Default.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Set<UserSecurity>().AsNoTracking().SingleAsync(s => s.UserId == userId);
    }

    [Fact]
    public async Task FailuresBelowTheMaximum_AreCounted_WithoutLocking()
    {
        var userId = await Seed.CreateUserAsync();

        await RecordFailure(userId, Now);
        await RecordFailure(userId, Now);

        var security = await SecurityOf(userId);
        security.FailedLoginAttempts.Should().Be(2);
        security.LockedUntilUtc.Should().BeNull();
        security.LockoutReason.Should().BeNull();
    }

    [Fact]
    public async Task TheMaximumFailure_LocksTheAccount_ForTheLockoutDuration()
    {
        var userId = await Seed.CreateUserAsync();

        for (var i = 0; i < MaxAttempts; i++) await RecordFailure(userId, Now);

        var security = await SecurityOf(userId);
        security.FailedLoginAttempts.Should().Be(MaxAttempts);
        security.LockedUntilUtc.Should().Be(Now.Add(Lockout));
        security.LockoutReason.Should().Be($"{MaxAttempts} consecutive failed login attempts");
    }

    [Fact]
    public async Task FailuresDuringAnActiveLockout_AreIgnored()
    {
        var userId = await Seed.CreateUserAsync();
        for (var i = 0; i < MaxAttempts; i++) await RecordFailure(userId, Now);

        await RecordFailure(userId, Now.AddMinutes(5));
        await RecordFailure(userId, Now.AddMinutes(10));

        var security = await SecurityOf(userId);
        security.FailedLoginAttempts.Should().Be(MaxAttempts);
        security.LockedUntilUtc.Should().Be(Now.Add(Lockout));
    }

    [Fact]
    public async Task TheFirstFailureAfterTheLockoutExpires_StartsANewCount_AndDoesNotRelock()
    {
        var userId = await Seed.CreateUserAsync();
        for (var i = 0; i < MaxAttempts; i++) await RecordFailure(userId, Now);

        await RecordFailure(userId, Now.Add(Lockout).AddMinutes(1));

        var security = await SecurityOf(userId);
        security.FailedLoginAttempts.Should().Be(1);
        security.LockedUntilUtc.Should().BeNull();
        security.LockoutReason.Should().BeNull();
    }

    [Fact]
    public async Task AFullRoundOfFailuresAfterAnExpiredLockout_LocksAgain()
    {
        var userId = await Seed.CreateUserAsync();
        for (var i = 0; i < MaxAttempts; i++) await RecordFailure(userId, Now);
        var afterLockout = Now.Add(Lockout).AddMinutes(1);

        for (var i = 0; i < MaxAttempts; i++) await RecordFailure(userId, afterLockout);

        var security = await SecurityOf(userId);
        security.FailedLoginAttempts.Should().Be(MaxAttempts);
        security.LockedUntilUtc.Should().Be(afterLockout.Add(Lockout));
    }

    [Fact]
    public async Task ConcurrentFailures_AreAllCounted()
    {
        var userId = await Seed.CreateUserAsync();

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => RecordFailure(userId, Now, maxAttempts: 100)));

        (await SecurityOf(userId)).FailedLoginAttempts.Should().Be(10);
    }
}
