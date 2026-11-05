using Shortly.Domain.ShortUrls;
using Shortly.Domain.UnitTests.TestData;

namespace Shortly.Domain.UnitTests.ShortUrls;

public class ShortUrlTests
{
    private static readonly DateTime Now = TestClock.Now;

    // ---- creation ----------------------------------------------------------------------------------------------------

    [Fact]
    public void CreateForUser_Should_SetOwnershipAndDefaults()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = ShortUrl.CreateForUser(userId, ShortUrlData.Url(), ShortUrlData.Code(), Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var link = result.Value;
        link.OwnerType.Should().Be(enShortUrlOwnerType.User);
        link.UserId.Should().Be(userId);
        link.OrganizationId.Should().BeNull();
        link.CreatedByMemberId.Should().BeNull();
        link.IsActive.Should().BeTrue();
        link.TrackingEnabled.Should().BeTrue();
        link.ClickLimit.Should().Be(-1);
        link.TotalClicks.Should().Be(0);
        link.IsPasswordProtected.Should().BeFalse();
        link.PasswordHash.Should().BeNull();
        link.CreatedAtUtc.Should().Be(Now);
        link.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void CreateForUser_Should_StoreTheOptionalSettings()
    {
        // Arrange
        var expiresAt = Now.AddDays(1);

        // Act
        var link = ShortUrl.CreateForUser(Guid.NewGuid(), ShortUrlData.Url(), ShortUrlData.Code(), Now,
            clickLimit: 5, trackingEnabled: false, isPrivate: true, expiresAt: expiresAt,
            title: "Title", description: "Description").Value;

        // Assert
        link.ClickLimit.Should().Be(5);
        link.TrackingEnabled.Should().BeFalse();
        link.IsPrivate.Should().BeTrue();
        link.ExpiresAt.Should().Be(expiresAt);
        link.Title.Should().Be("Title");
        link.Description.Should().Be("Description");
    }

    [Fact]
    public void CreateForOrganization_Should_SetOrganizationOwnershipAndCreator()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        // Act
        var result = ShortUrl.CreateForOrganization(organizationId, memberId, ShortUrlData.Url(),
            ShortUrlData.Code(), Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OwnerType.Should().Be(enShortUrlOwnerType.Organization);
        result.Value.OrganizationId.Should().Be(organizationId);
        result.Value.CreatedByMemberId.Should().Be(memberId);
        result.Value.UserId.Should().BeNull();
    }

    [Fact]
    public void CreateForAnonymous_Should_SetSessionAndIpWithoutTrackingOrLimits()
    {
        // Act
        var result = ShortUrl.CreateForAnonymous("session-1", "203.0.113.5", ShortUrlData.Url(),
            ShortUrlData.Code(), Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var link = result.Value;
        link.OwnerType.Should().Be(enShortUrlOwnerType.Anonymous);
        link.AnonymousSessionId.Should().Be("session-1");
        link.IpAddress.Should().Be("203.0.113.5");
        link.UserId.Should().BeNull();
        link.OrganizationId.Should().BeNull();
        link.TrackingEnabled.Should().BeFalse();
        link.ClickLimit.Should().Be(-1);
        link.ExpiresAt.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateForAnonymous_Should_Throw_When_SessionIdIsMissing(string? sessionId)
    {
        // Act
        var act = () => ShortUrl.CreateForAnonymous(sessionId!, "203.0.113.5", ShortUrlData.Url(),
            ShortUrlData.Code(), Now);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("anonymousSessionId");
    }

    [Fact]
    public void CreateForAnonymous_Should_Succeed_When_IpAddressIsUnavailable()
    {
        // Act
        var result = ShortUrl.CreateForAnonymous("session-1", null, ShortUrlData.Url(), ShortUrlData.Code(), Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IpAddress.Should().BeNull();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(1000)]
    public void CreateForUser_Should_Succeed_When_ClickLimitIsValid(int clickLimit)
    {
        // Act
        var result = ShortUrl.CreateForUser(Guid.NewGuid(), ShortUrlData.Url(), ShortUrlData.Code(), Now, clickLimit);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ClickLimit.Should().Be(clickLimit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(int.MinValue)]
    public void CreateForUser_Should_Fail_When_ClickLimitIsInvalid(int clickLimit)
    {
        // Act
        var result = ShortUrl.CreateForUser(Guid.NewGuid(), ShortUrlData.Url(), ShortUrlData.Code(), Now, clickLimit);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidClickLimit);
    }

    [Fact]
    public void CreateForOrganization_Should_Fail_When_ClickLimitIsInvalid()
    {
        // Act
        var result = ShortUrl.CreateForOrganization(Guid.NewGuid(), Guid.NewGuid(), ShortUrlData.Url(),
            ShortUrlData.Code(), Now, clickLimit: 0);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidClickLimit);
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(0)]
    public void CreateForUser_Should_Fail_When_ExpirationIsNotInTheFuture(int secondsFromNow)
    {
        // Act
        var result = ShortUrl.CreateForUser(Guid.NewGuid(), ShortUrlData.Url(), ShortUrlData.Code(), Now,
            expiresAt: Now.AddSeconds(secondsFromNow));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidExpiration);
    }

    [Fact]
    public void CreateForOrganization_Should_Fail_When_ExpirationIsInThePast()
    {
        // Act
        var result = ShortUrl.CreateForOrganization(Guid.NewGuid(), Guid.NewGuid(), ShortUrlData.Url(),
            ShortUrlData.Code(), Now, expiresAt: Now.AddMinutes(-1));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidExpiration);
    }

    [Fact]
    public void CreateForUser_Should_ReportTheExpirationError_When_BothExpirationAndClickLimitAreInvalid()
    {
        // Act
        var result = ShortUrl.CreateForUser(Guid.NewGuid(), ShortUrlData.Url(), ShortUrlData.Code(), Now,
            clickLimit: 0, expiresAt: Now.AddDays(-1));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidExpiration);
    }

    // ---- update ------------------------------------------------------------------------------------------------------

    [Fact]
    public void Update_Should_ReplaceTheEditableProperties()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        var later = Now.AddHours(1);
        var newUrl = ShortUrlData.Url("https://example.org/new");
        var newCode = ShortUrlData.Code("newcode1");

        // Act
        var result = link.Update(newUrl, newCode, "Title", "Description", clickLimit: 10, isActive: false,
            isPrivate: true, isPasswordProtected: true, passwordHash: "hash", trackingEnabled: false,
            expiresAt: null, later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.OriginalUrl.Should().Be(newUrl);
        link.ShortCode.Should().Be(newCode);
        link.Title.Should().Be("Title");
        link.Description.Should().Be("Description");
        link.ClickLimit.Should().Be(10);
        link.IsActive.Should().BeFalse();
        link.IsPrivate.Should().BeTrue();
        link.IsPasswordProtected.Should().BeTrue();
        link.PasswordHash.Should().Be("hash");
        link.TrackingEnabled.Should().BeFalse();
        link.UpdatedAtUtc.Should().Be(later);
    }

    [Fact]
    public void Update_Should_RemoveTheProtection_When_NotPasswordProtected()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        link.ProtectWithPassword("hash", Now);

        // Act
        var result = UpdateWith(link, isPasswordProtected: false, passwordHash: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.IsPasswordProtected.Should().BeFalse();
        link.PasswordHash.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Update_Should_Fail_When_ClickLimitIsInvalid(int clickLimit)
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();

        // Act
        var result = UpdateWith(link, clickLimit: clickLimit);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidClickLimit);
    }

    [Fact]
    public void Update_Should_Fail_When_ExpirationIsInThePast()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();

        // Act
        var result = UpdateWith(link, expiresAt: Now.AddMinutes(-1));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidExpiration);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_Should_Fail_When_ProtectedWithoutAHash(string? passwordHash)
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();

        // Act
        var result = UpdateWith(link, isPasswordProtected: true, passwordHash: passwordHash);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.PasswordRequired);
    }

    [Fact]
    public void Update_Should_LeaveTheLinkUnchanged_When_ValidationFails()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink(clickLimit: 5);
        var originalUrl = link.OriginalUrl;

        // Act
        var result = link.Update(ShortUrlData.Url("https://example.org/other"), ShortUrlData.Code("other123"),
            "Changed", null, clickLimit: 0, isActive: false, isPrivate: true, isPasswordProtected: false,
            passwordHash: null, trackingEnabled: false, expiresAt: null, Now.AddHours(1));

        // Assert
        result.IsFailure.Should().BeTrue();
        link.OriginalUrl.Should().Be(originalUrl);
        link.Title.Should().BeNull();
        link.ClickLimit.Should().Be(5);
        link.IsActive.Should().BeTrue();
        link.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Update_Should_AssignTheExpirationDate()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        var expiresAt = Now.AddDays(7);

        // Act
        var result = UpdateWith(link, expiresAt: expiresAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void Update_Should_ClearTheExpirationDate_When_NullIsPassed()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink(expiresAt: Now.AddDays(1));

        // Act
        var result = UpdateWith(link, expiresAt: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void Update_Should_DiscardTheHash_When_NotPasswordProtected()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();

        // Act
        var result = UpdateWith(link, isPasswordProtected: false, passwordHash: "stray-hash");

        // Assert: the flag and the stored hash can never disagree
        result.IsSuccess.Should().BeTrue();
        link.IsPasswordProtected.Should().BeFalse();
        link.PasswordHash.Should().BeNull();
    }

    // ---- short code assignment -----------------------------------------------------------------------------------------

    [Fact]
    public void AssignShortCode_Should_ReplaceTheCodeAndTouchTheTimestamp()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        var newCode = ShortUrlData.Code("fresh123");
        var later = Now.AddMinutes(5);

        // Act
        var result = link.AssignShortCode(newCode, later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.ShortCode.Should().Be(newCode);
        link.UpdatedAtUtc.Should().Be(later);
    }

    // ---- activation --------------------------------------------------------------------------------------------------

    [Fact]
    public void Deactivate_Should_MakeTheLinkInactive()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();

        // Act
        var result = link.Deactivate(Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_Should_Fail_When_AlreadyInactive()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        link.Deactivate(Now);

        // Act
        var result = link.Deactivate(Now);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.AlreadyInactive);
    }

    [Fact]
    public void Activate_Should_MakeTheLinkActive()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        link.Deactivate(Now);

        // Act
        var result = link.Activate(Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_Should_Fail_When_AlreadyActive()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();

        // Act
        var result = link.Activate(Now);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.AlreadyActive);
    }

    [Fact]
    public void Activate_Should_TouchUpdatedAtUtc()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        link.Deactivate(Now.AddMinutes(1));
        var later = Now.AddMinutes(2);

        // Act
        link.Activate(later);

        // Assert
        link.UpdatedAtUtc.Should().Be(later);
    }

    [Fact]
    public void Deactivate_Should_TouchUpdatedAtUtc()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        var later = Now.AddMinutes(1);

        // Act
        link.Deactivate(later);

        // Assert
        link.UpdatedAtUtc.Should().Be(later);
    }

    [Fact]
    public void Activate_Should_NotTouchUpdatedAtUtc_When_ItFails()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();

        // Act
        var result = link.Activate(Now.AddMinutes(1));

        // Assert
        result.IsFailure.Should().BeTrue();
        link.UpdatedAtUtc.Should().Be(Now);
    }

    // ---- expiration --------------------------------------------------------------------------------------------------

    [Fact]
    public void SetExpiration_Should_StoreAFutureDate()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        var expiresAt = Now.AddDays(3);

        // Act
        var result = link.SetExpiration(expiresAt, Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.ExpiresAt.Should().Be(expiresAt);
        link.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void SetExpiration_Should_TouchUpdatedAtUtc()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        var later = Now.AddMinutes(3);

        // Act
        link.SetExpiration(Now.AddDays(1), later);

        // Assert
        link.UpdatedAtUtc.Should().Be(later);
    }

    [Fact]
    public void SetExpiration_Should_ClearTheExpiration_When_NullIsPassed()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink(expiresAt: Now.AddDays(1));

        // Act
        var result = link.SetExpiration(null, Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.ExpiresAt.Should().BeNull();
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(0)]
    public void SetExpiration_Should_Fail_When_DateIsNotInTheFuture(int secondsFromNow)
    {
        // Arrange
        var existing = Now.AddDays(1);
        var link = ShortUrlData.CreateUserLink(expiresAt: existing);

        // Act
        var result = link.SetExpiration(Now.AddSeconds(secondsFromNow), Now);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.InvalidExpiration);
        link.ExpiresAt.Should().Be(existing);
    }

    [Fact]
    public void IsExpired_Should_BeFalse_When_NoExpirationIsSet()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();

        // Assert
        link.IsExpired(Now.AddYears(50)).Should().BeFalse();
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void IsExpired_Should_TreatTheExactExpiryInstantAsExpired(int secondsRelativeToExpiry, bool expected)
    {
        // Arrange
        var expiresAt = Now.AddHours(1);
        var link = ShortUrlData.CreateUserLink(expiresAt: expiresAt);

        // Act
        var expired = link.IsExpired(expiresAt.AddSeconds(secondsRelativeToExpiry));

        // Assert
        expired.Should().Be(expected);
    }

    // ---- password ----------------------------------------------------------------------------------------------------

    [Fact]
    public void ProtectWithPassword_Should_SetTheFlagAndHash()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();

        // Act
        var result = link.ProtectWithPassword("hash", Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.IsPasswordProtected.Should().BeTrue();
        link.PasswordHash.Should().Be("hash");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ProtectWithPassword_Should_Fail_When_HashIsEmpty(string? hash)
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();

        // Act
        var result = link.ProtectWithPassword(hash!, Now);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.PasswordRequired);
        link.IsPasswordProtected.Should().BeFalse();
    }

    [Fact]
    public void ProtectWithPassword_Should_ReplaceTheHash_When_AlreadyProtected()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        link.ProtectWithPassword("old", Now);

        // Act
        link.ProtectWithPassword("new", Now);

        // Assert
        link.PasswordHash.Should().Be("new");
    }

    [Fact]
    public void RemovePassword_Should_ClearTheFlagAndHash()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        link.ProtectWithPassword("hash", Now);

        // Act
        var result = link.RemovePassword(Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.IsPasswordProtected.Should().BeFalse();
        link.PasswordHash.Should().BeNull();
    }

    [Fact]
    public void ProtectWithPassword_Should_TouchUpdatedAtUtc()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        var later = Now.AddMinutes(4);

        // Act
        link.ProtectWithPassword("hash", later);

        // Assert
        link.UpdatedAtUtc.Should().Be(later);
    }

    [Fact]
    public void RemovePassword_Should_TouchUpdatedAtUtc()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        link.ProtectWithPassword("hash", Now.AddMinutes(1));
        var later = Now.AddMinutes(5);

        // Act
        link.RemovePassword(later);

        // Assert
        link.UpdatedAtUtc.Should().Be(later);
    }

    [Fact]
    public void RemovePassword_Should_Fail_When_NotProtected()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();

        // Act
        var result = link.RemovePassword(Now);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.NotPasswordProtected);
    }

    // ---- clicks and accessibility ------------------------------------------------------------------------------------

    [Fact]
    public void RecordClick_Should_IncrementTheCounterAndTouchTheTimestamp()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        var later = Now.AddMinutes(1);

        // Act
        var result = link.RecordClick(later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        link.TotalClicks.Should().Be(1);
        link.UpdatedAtUtc.Should().Be(later);
    }

    [Fact]
    public void RecordClick_Should_NeverReachTheLimit_When_ClickLimitIsUnlimited()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink(clickLimit: -1);

        // Act
        for (var i = 0; i < 500; i++) link.RecordClick(Now);

        // Assert
        link.TotalClicks.Should().Be(500);
        link.HasReachedClickLimit().Should().BeFalse();
    }

    [Fact]
    public void RecordClick_Should_Fail_When_ClickLimitIsReached()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink(clickLimit: 2);
        link.RecordClick(Now);
        link.RecordClick(Now);

        // Act
        var result = link.RecordClick(Now);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ClickLimitReached);
        link.TotalClicks.Should().Be(2);
    }

    [Fact]
    public void RecordClick_Should_Fail_When_Inactive()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink();
        link.Deactivate(Now);

        // Act
        var result = link.RecordClick(Now);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.Inactive);
        link.TotalClicks.Should().Be(0);
    }

    [Fact]
    public void RecordClick_Should_Fail_When_Expired()
    {
        // Arrange
        var expiresAt = Now.AddHours(1);
        var link = ShortUrlData.CreateUserLink(expiresAt: expiresAt);

        // Act
        var result = link.RecordClick(expiresAt.AddSeconds(1));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.Expired);
        link.TotalClicks.Should().Be(0);
    }

    [Theory]
    [InlineData(2, 1, false)]
    [InlineData(2, 2, true)]
    [InlineData(1, 5, true)]
    [InlineData(-1, 1000, false)]
    public void HasReachedClickLimit_Should_CompareTotalClicksWithTheLimit(int limit, int clicks, bool expected)
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink(clickLimit: limit);
        for (var i = 0; i < clicks && !link.HasReachedClickLimit(); i++) link.RecordClick(Now);

        // Assert
        link.HasReachedClickLimit().Should().Be(expected);
    }

    [Fact]
    public void EnsureAccessible_Should_Succeed_When_ActiveNotExpiredAndUnderTheLimit()
    {
        // Arrange
        var link = ShortUrlData.CreateUserLink(clickLimit: 3, expiresAt: Now.AddDays(1));

        // Act
        var result = link.EnsureAccessible(Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void EnsureAccessible_Should_ReportInactive_BeforeExpiredAndClickLimit()
    {
        // Arrange: inactive, expired and over the limit at the same time
        var expiresAt = Now.AddHours(1);
        var link = ShortUrlData.CreateUserLink(clickLimit: 1, expiresAt: expiresAt);
        link.RecordClick(Now);
        link.Deactivate(Now);

        // Act
        var result = link.EnsureAccessible(expiresAt.AddHours(1));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.Inactive);
    }

    [Fact]
    public void EnsureAccessible_Should_ReportExpired_BeforeClickLimit()
    {
        // Arrange
        var expiresAt = Now.AddHours(1);
        var link = ShortUrlData.CreateUserLink(clickLimit: 1, expiresAt: expiresAt);
        link.RecordClick(Now);

        // Act
        var result = link.EnsureAccessible(expiresAt.AddHours(1));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.Expired);
    }

    // ---- helpers -----------------------------------------------------------------------------------------------------

    private static Shortly.Domain.Abstractions.Result UpdateWith(
        ShortUrl link,
        int? clickLimit = null,
        bool isPasswordProtected = false,
        string? passwordHash = null,
        DateTime? expiresAt = null) =>
        link.Update(link.OriginalUrl, link.ShortCode, link.Title, link.Description,
            clickLimit ?? link.ClickLimit, link.IsActive, link.IsPrivate, isPasswordProtected, passwordHash,
            link.TrackingEnabled, expiresAt, Now.AddMinutes(1));
}
