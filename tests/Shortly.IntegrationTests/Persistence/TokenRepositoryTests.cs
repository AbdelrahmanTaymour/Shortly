using Microsoft.EntityFrameworkCore;
using Shortly.Core.Exceptions;
using Shortly.Domain.RefreshToken;
using Shortly.Domain.Tokens;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Persistence;

public class TokenRepositoryTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task<RefreshToken> ReadRefresh(Guid id) => Query(db => db.Set<RefreshToken>().SingleAsync(t => t.Id == id));
    private Task<UserActionToken> ReadAction(Guid id) => Query(db => db.Set<UserActionToken>().SingleAsync(t => t.Id == id));

    // ---- refresh tokens ------------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetByTokenHashAsync_Should_LoadTheTokenWithItsUser()
    {
        // Arrange
        var user = await Data.UserAsync();
        var token = await Data.RefreshTokenAsync(user.Id);

        // Act
        var loaded = await WithRepository<IRefreshTokenRepository, RefreshToken?>(r => r.GetByTokenHashAsync(token.TokenHash), save: false);

        // Assert
        loaded!.User.Id.Should().Be(user.Id);
        loaded.User.Profile.Should().NotBeNull();
        loaded.User.Security.Should().NotBeNull();
    }

    [Fact]
    public async Task RotateAsync_Should_SwapTheHash_AndExtendTheLifetime_WhenTheCurrentHashMatches()
    {
        // Arrange
        var user = await Data.UserAsync();
        var token = await Data.RefreshTokenAsync(user.Id, lifetime: TimeSpan.FromDays(1));
        var now = TestData.Now;

        // Act
        var rotated = await WithRepository<IRefreshTokenRepository, bool>(
            r => r.RotateAsync(token.Id, token.TokenHash, "new-hash-1", TimeSpan.FromDays(15), now), save: false);

        // Assert
        rotated.Should().BeTrue();
        var stored = await ReadRefresh(token.Id);
        stored.TokenHash.Should().Be("new-hash-1");
        stored.ExpiresAt.Should().BeCloseTo(now.AddDays(15), TimeSpan.FromSeconds(1));
        stored.UpdatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RotateAsync_Should_Refuse_AWrongHash_ARevokedToken_OrAnExpiredOne()
    {
        // Arrange
        var user = await Data.UserAsync();
        var token = await Data.RefreshTokenAsync(user.Id);
        var revoked = await Data.RefreshTokenAsync(user.Id);
        await WithRepository<IRefreshTokenRepository, bool>(r => r.RevokeAsync(revoked.TokenHash, TestData.Now), save: false);
        var expired = await Data.RefreshTokenAsync(user.Id, lifetime: TimeSpan.FromMinutes(1));

        // Act
        Task<bool> Rotate(RefreshToken t, string current, DateTime at) =>
            WithRepository<IRefreshTokenRepository, bool>(r => r.RotateAsync(t.Id, current, "new", TimeSpan.FromDays(1), at), save: false);

        // Assert
        (await Rotate(token, "wrong-hash", TestData.Now)).Should().BeFalse();
        (await Rotate(revoked, revoked.TokenHash, TestData.Now)).Should().BeFalse();
        (await Rotate(expired, expired.TokenHash, TestData.Now.AddHours(1))).Should().BeFalse();
        (await ReadRefresh(token.Id)).TokenHash.Should().Be(token.TokenHash);
    }

    [Fact]
    public async Task RotateAsync_Should_LetExactlyOneOfManyConcurrentRequestsWin()
    {
        // Arrange: a stolen or double-submitted refresh token must only be usable once
        var user = await Data.UserAsync();
        var token = await Data.RefreshTokenAsync(user.Id);

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => WithRepository<IRefreshTokenRepository, bool>(
            r => r.RotateAsync(token.Id, token.TokenHash, $"rotated-{i}", TimeSpan.FromDays(1), TestData.Now), save: false)));

        // Assert
        results.Count(won => won).Should().Be(1);
    }

    [Fact]
    public async Task RevokeAsync_Should_RevokeOnce_AndReportFalseTheSecondTime()
    {
        // Arrange
        var user = await Data.UserAsync();
        var token = await Data.RefreshTokenAsync(user.Id);

        // Act
        var first = await WithRepository<IRefreshTokenRepository, bool>(r => r.RevokeAsync(token.TokenHash, TestData.Now), save: false);
        var second = await WithRepository<IRefreshTokenRepository, bool>(r => r.RevokeAsync(token.TokenHash, TestData.Now), save: false);

        // Assert
        first.Should().BeTrue();
        second.Should().BeFalse();
        var stored = await ReadRefresh(token.Id);
        stored.IsRevoked.Should().BeTrue();
        stored.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeAllAsync_Should_RevokeEveryActiveTokenOfThatUserOnly()
    {
        // Arrange
        var user = await Data.UserAsync();
        var other = await Data.UserAsync();
        var a = await Data.RefreshTokenAsync(user.Id);
        var b = await Data.RefreshTokenAsync(user.Id);
        var othersToken = await Data.RefreshTokenAsync(other.Id);

        // Act
        var count = await WithRepository<IRefreshTokenRepository, int>(r => r.RevokeAllAsync(user.Id, TestData.Now), save: false);

        // Assert
        count.Should().Be(2);
        (await ReadRefresh(a.Id)).IsRevoked.Should().BeTrue();
        (await ReadRefresh(b.Id)).IsRevoked.Should().BeTrue();
        (await ReadRefresh(othersToken.Id)).IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task CleanupExpiredAsync_Should_DeleteOnlyExpiredTokens()
    {
        // Arrange
        var user = await Data.UserAsync();
        var expired = await Data.RefreshTokenAsync(user.Id, lifetime: TimeSpan.FromMinutes(1));
        var live = await Data.RefreshTokenAsync(user.Id);

        // Act
        await WithRepository<IRefreshTokenRepository, int>(r => r.CleanupExpiredAsync(TestData.Now.AddHours(1)), save: false);

        // Assert
        (await Query(db => db.Set<RefreshToken>().AnyAsync(t => t.Id == expired.Id))).Should().BeFalse();
        (await Query(db => db.Set<RefreshToken>().AnyAsync(t => t.Id == live.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task Saving_Should_Fail_ForADuplicateRefreshTokenHash()
    {
        // Arrange
        var user = await Data.UserAsync();
        var token = await Data.RefreshTokenAsync(user.Id);

        // Act
        var duplicate = () => Data.RefreshTokenAsync(user.Id, tokenHash: token.TokenHash);

        // Assert
        await duplicate.Should().ThrowAsync<UniqueConstraintViolationException>();
    }

    // ---- action tokens -------------------------------------------------------------------------------------------------

    [Fact]
    public async Task InvalidateActiveTokensAsync_Should_MarkOnlyTheUsersUnusedTokensOfThatTypeAsUsed()
    {
        // Arrange
        var user = await Data.UserAsync();
        var other = await Data.UserAsync();
        var reset = await Data.PasswordResetTokenAsync(user.Id);
        var verification = UserActionToken.IssueEmailVerification(user.Id, $"ev-{Guid.NewGuid():N}", TestData.Now).Value;
        await Seed(db => { db.Add(verification); return Task.CompletedTask; });
        var othersReset = await Data.PasswordResetTokenAsync(other.Id);

        // Act
        var count = await WithRepository<IUserActionTokenRepository, int>(
            r => r.InvalidateActiveTokensAsync(user.Id, enUserActionTokenType.PasswordReset, TestData.Now), save: false);

        // Assert
        count.Should().Be(1);
        (await ReadAction(reset.Id)).Used.Should().BeTrue();
        (await ReadAction(reset.Id)).UsedAt.Should().NotBeNull();
        (await ReadAction(verification.Id)).Used.Should().BeFalse();
        (await ReadAction(othersReset.Id)).Used.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteExpiredOrUsedAsync_Should_RemoveUsedAndExpiredTokens_AndKeepLiveOnes()
    {
        // Arrange
        var user = await Data.UserAsync();
        var live = await Data.PasswordResetTokenAsync(user.Id);
        var expired = await Data.PasswordResetTokenAsync(user.Id, issuedAt: TestData.Now.AddDays(-5));
        var used = await Data.PasswordResetTokenAsync(user.Id);
        await WithRepository<IUserActionTokenRepository, int>(
            r => r.InvalidateActiveTokensAsync(user.Id, enUserActionTokenType.PasswordReset, TestData.Now), save: false);
        var stillLive = await Data.PasswordResetTokenAsync(user.Id);

        // Act
        await WithRepository<IUserActionTokenRepository, int>(r => r.DeleteExpiredOrUsedAsync(TestData.Now), save: false);

        // Assert
        (await Query(db => db.Set<UserActionToken>().AnyAsync(t => t.Id == expired.Id))).Should().BeFalse();
        (await Query(db => db.Set<UserActionToken>().AnyAsync(t => t.Id == used.Id))).Should().BeFalse();
        (await Query(db => db.Set<UserActionToken>().AnyAsync(t => t.Id == live.Id))).Should().BeFalse(); // invalidated with the others
        (await Query(db => db.Set<UserActionToken>().AnyAsync(t => t.Id == stillLive.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task GetByTokenHashAsync_Should_FindAnActionToken()
    {
        // Arrange
        var user = await Data.UserAsync();
        var token = await Data.PasswordResetTokenAsync(user.Id);

        // Act
        var found = await WithRepository<IUserActionTokenRepository, UserActionToken?>(r => r.GetByTokenHashAsync(token.TokenHash), save: false);

        // Assert
        found!.Id.Should().Be(token.Id);
    }
}
