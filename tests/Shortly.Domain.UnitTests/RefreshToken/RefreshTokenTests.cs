using Shortly.Domain.UnitTests.TestData;
using Shortly.Domain.Users;
using RefreshTokenEntity = Shortly.Domain.RefreshToken.RefreshToken;

namespace Shortly.Domain.UnitTests.RefreshToken;

public class RefreshTokenTests
{
    private static readonly DateTime Now = TestClock.Now;
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private static RefreshTokenEntity NewToken(Guid? organizationId = null) =>
        RefreshTokenEntity.Issue(Guid.NewGuid(), "hash", Lifetime, organizationId, Now);

    [Fact]
    public void Issue_Should_CreateAnActiveToken_ExpiringAfterTheLifetime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        // Act
        var token = RefreshTokenEntity.Issue(userId, "hash", Lifetime, organizationId, Now);

        // Assert
        token.Id.Should().NotBeEmpty();
        token.UserId.Should().Be(userId);
        token.TokenHash.Should().Be("hash");
        token.OrganizationId.Should().Be(organizationId);
        token.ExpiresAt.Should().Be(Now.Add(Lifetime));
        token.CreatedAt.Should().Be(Now);
        token.IsRevoked.Should().BeFalse();
        token.RevokedAt.Should().BeNull();
        token.UpdatedAt.Should().BeNull();
        token.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void Issue_Should_AllowAPersonalWorkspaceToken_WithoutAnOrganization()
    {
        // Act
        var token = NewToken(organizationId: null);

        // Assert
        token.OrganizationId.Should().BeNull();
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void IsExpired_Should_TreatTheExactExpiryInstantAsExpired(int secondsRelativeToExpiry, bool expected)
    {
        // Arrange
        var token = NewToken();

        // Act
        var expired = token.IsExpired(token.ExpiresAt.AddSeconds(secondsRelativeToExpiry));

        // Assert
        expired.Should().Be(expected);
    }

    [Fact]
    public void IsActive_Should_BeFalse_WhenTheTokenIsExpired()
    {
        // Arrange
        var token = NewToken();

        // Assert
        token.IsActive(token.ExpiresAt).Should().BeFalse();
    }

    [Fact]
    public void Revoke_Should_FlagTheTokenRevoked_AndMakeItInactive()
    {
        // Arrange
        var token = NewToken();
        var later = Now.AddHours(1);

        // Act
        token.Revoke(later);

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().Be(later);
        token.UpdatedAt.Should().Be(later);
        token.IsActive(later).Should().BeFalse();
    }

    [Fact]
    public void Revoke_Should_BeIdempotent_AndKeepTheFirstRevocationTime()
    {
        // Arrange
        var token = NewToken();
        token.Revoke(Now.AddHours(1));

        // Act
        token.Revoke(Now.AddHours(2));

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().Be(Now.AddHours(1));
        token.UpdatedAt.Should().Be(Now.AddHours(1));
    }

    [Fact]
    public void Rotate_Should_ReplaceTheHashAndExtendTheExpiry()
    {
        // Arrange
        var token = NewToken();
        var later = Now.AddDays(3);

        // Act
        var result = token.Rotate("new-hash", Lifetime, later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        token.TokenHash.Should().Be("new-hash");
        token.ExpiresAt.Should().Be(later.Add(Lifetime));
        token.UpdatedAt.Should().Be(later);
        token.IsActive(later).Should().BeTrue();
    }

    [Fact]
    public void Rotate_Should_Fail_AndChangeNothing_ForARevokedToken()
    {
        // Arrange
        var token = NewToken();
        token.Revoke(Now);
        var expiresAt = token.ExpiresAt;

        // Act
        var result = token.Rotate("new-hash", Lifetime, Now.AddMinutes(1));

        // Assert
        result.Error.Should().Be(UserErrors.InvalidRefreshToken);
        token.TokenHash.Should().Be("hash");
        token.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void Rotate_Should_Fail_AndChangeNothing_ForAnExpiredToken_AtTheExactExpiryInstant()
    {
        // Arrange
        var token = NewToken();

        // Act
        var result = token.Rotate("new-hash", Lifetime, token.ExpiresAt);

        // Assert
        result.Error.Should().Be(UserErrors.InvalidRefreshToken);
        token.TokenHash.Should().Be("hash");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public void Rotate_Should_Throw_When_TheLifetimeIsNotPositive(int seconds)
    {
        // Arrange
        var token = NewToken();

        // Act
        var act = () => token.Rotate("new-hash", TimeSpan.FromSeconds(seconds), Now);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
        token.TokenHash.Should().Be("hash");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public void Issue_Should_Throw_When_TheLifetimeIsNotPositive(int seconds)
    {
        // Act
        var act = () => RefreshTokenEntity.Issue(Guid.NewGuid(), "hash", TimeSpan.FromSeconds(seconds), null, Now);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("lifetime");
    }
}
