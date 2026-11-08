using Microsoft.EntityFrameworkCore;
using Shortly.Core.Exceptions;
using Shortly.Core.Users.DeleteAccount;
using Shortly.Core.Users.GetLoggedInUser;
using Shortly.Core.Users.GetMyProfile;
using Shortly.Core.Users.GetMyQuotaStatus;
using Shortly.Core.Users.UpdateProfile;
using Shortly.Domain.RefreshToken;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Users;

public class UserSelfServiceTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task<User> Stored(Guid id) => Query(db => db.Set<User>().Include(u => u.Profile).SingleAsync(u => u.Id == id));
 
    private static UpdateProfileCommand Patch(
        string? name = null, string? bio = null, string? phoneNumber = null, string? website = null,
        string? company = null, string? location = null, string? country = null, string? timeZone = null) =>
        new(name, bio, phoneNumber, website, company, location, country, timeZone);
 
    // ---- GetLoggedInUser ------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task GetLoggedInUser_Should_ReturnTheCallersIdentityAndPlan()
    {
        // Arrange
        var user = await Data.UserAsync(emailConfirmed: false, plan: enSubscriptionPlan.Starter);
        As(user.Id);
 
        // Act
        var result = await Send(new GetLoggedInUserQuery());
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
        result.Value.Username.Should().Be(user.Username);
        result.Value.Role.Should().Be(UserRole.User);
        result.Value.SubscriptionPlan.Should().Be(enSubscriptionPlan.Starter);
        result.Value.IsEmailConfirmed.Should().BeFalse();
    }
 
    [Fact]
    public async Task GetLoggedInUser_Should_ReturnNotFound_ForASoftDeletedUser()
    {
        // Arrange
        var user = await Data.UserAsync();
        await Seed(async db => (await db.Set<User>().SingleAsync(u => u.Id == user.Id)).SoftDelete(user.Id, TestData.Now));
        As(user.Id);
 
        // Act
        var result = await Send(new GetLoggedInUserQuery());
 
        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
    }
 
    // ---- GetMyProfile -----------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task GetMyProfile_Should_ReturnTheAccountFields_AndTheBlankProfile_ForANewUser()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);
 
        // Act
        var result = await Send(new GetMyProfileQuery());
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
        result.Value.Username.Should().Be(user.Username);
        result.Value.IsEmailConfirmed.Should().BeTrue();
        result.Value.Name.Should().BeNull();
        result.Value.Bio.Should().BeNull();
    }
 
    [Fact]
    public async Task GetMyProfile_Should_ReflectAnEarlierUpdate()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);
        await Send(Patch(name: "Ada Lovelace", bio: "Builder of things", company: "Acme"));
 
        // Act
        var result = await Send(new GetMyProfileQuery());
 
        // Assert
        result.Value.Name.Should().Be("Ada Lovelace");
        result.Value.Bio.Should().Be("Builder of things");
        result.Value.Company.Should().Be("Acme");
    }
 
    // ---- UpdateProfile ------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task UpdateProfile_Should_SetEveryField_ForTheCaller()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);
 
        // Act
        var result = await Send(Patch("Ada Lovelace", "Mathematician", "+15551234567", "https://ada.dev", "Analytical Engines Ltd", "London", "UK", "Europe/London"));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await Stored(user.Id);
        stored.Profile.Name.Should().Be("Ada Lovelace");
        stored.Profile.Bio.Should().Be("Mathematician");
        stored.Profile.PhoneNumber.Should().Be("+15551234567");
        stored.Profile.Website.Should().Be("https://ada.dev");
        stored.Profile.Company.Should().Be("Analytical Engines Ltd");
        stored.Profile.Location.Should().Be("London");
        stored.Profile.Country.Should().Be("UK");
        stored.Profile.TimeZone.Should().Be("Europe/London");
    }
 
    [Fact]
    public async Task UpdateProfile_Should_KeepFieldsLeftAsNull_ButClearFieldsSentBlank()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);
        await Send(Patch(name: "Ada Lovelace", bio: "Mathematician"));
 
        // Act: omit the name (null => unchanged) and blank out the bio (=> cleared)
        var result = await Send(Patch(bio: "   "));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await Stored(user.Id);
        stored.Profile.Name.Should().Be("Ada Lovelace");
        stored.Profile.Bio.Should().BeNull();
    }
 
    [Fact]
    public async Task UpdateProfile_Should_TrimWhitespace_AroundAProvidedValue()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);
 
        // Act
        await Send(Patch(name: "  Ada Lovelace  "));
 
        // Assert
        (await Stored(user.Id)).Profile.Name.Should().Be("Ada Lovelace");
    }
 
    [Fact]
    public async Task UpdateProfile_Should_RejectAFieldOverItsMaxLength_BeforeChangingAnything()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);
 
        // Act
        var act = () => Send(Patch(name: new string('a', 101)));
 
        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        (await Stored(user.Id)).Profile.Name.Should().BeNull();
    }
 
    [Fact]
    public async Task UpdateProfile_Should_ReturnNotFound_ForASoftDeletedUser()
    {
        // Arrange
        var user = await Data.UserAsync();
        await Seed(async db => (await db.Set<User>().SingleAsync(u => u.Id == user.Id)).SoftDelete(user.Id, TestData.Now));
        As(user.Id);
 
        // Act
        var result = await Send(Patch(name: "New name"));
 
        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
    }
 
    // ---- DeleteAccount ------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task DeleteAccount_Should_SoftDeleteTheCaller_AndRevokeTheirRefreshTokens()
    {
        // Arrange
        var user = await Data.UserAsync();
        var token = await Data.RefreshTokenAsync(user.Id);
        As(user.Id);
 
        // Act
        var result = await Send(new DeleteAccountCommand());
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await Stored(user.Id);
        stored.IsDeleted.Should().BeTrue();
        stored.IsActive.Should().BeFalse();
        stored.DeletedBy.Should().Be(user.Id);
        var storedToken = await Query(db => db.Set<RefreshToken>().SingleAsync(t => t.Id == token.Id));
        storedToken.IsRevoked.Should().BeTrue();
    }
 
    [Fact]
    public async Task DeleteAccount_Should_RefuseAUserWhoStillOwnsAnOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        As(org.Owner.Id);
 
        // Act
        var result = await Send(new DeleteAccountCommand());
 
        // Assert
        result.Error.Should().Be(UserErrors.OwnsOrganizations);
        (await Stored(org.Owner.Id)).IsDeleted.Should().BeFalse();
    }
 
    [Fact]
    public async Task DeleteAccount_Should_ReturnNotFound_ForASecondCall()
    {
        // Arrange: once deleted, the repository's own read no longer sees the user
        var user = await Data.UserAsync();
        As(user.Id);
        await Send(new DeleteAccountCommand());
 
        // Act
        var result = await Send(new DeleteAccountCommand());
 
        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
    }
 
    // ---- GetMyQuotaStatus ---------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task GetMyQuotaStatus_Should_ReportTheFreePlanLimits_ForABrandNewUser()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);
 
        // Act
        var result = await Send(new GetMyQuotaStatusQuery());
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MonthlyLinksCreated.Should().Be(0);
        result.Value.MonthlyLinksLimit.Should().Be(5); // Free plan
        result.Value.RemainingLinks.Should().Be(5);
        result.Value.CanCreateMoreLinks.Should().BeTrue();
        result.Value.MonthlyQrCodesLimit.Should().Be(2);
        result.Value.CanCreateMoreQrCodes.Should().BeTrue();
    }
 
    [Fact]
    public async Task GetMyQuotaStatus_Should_ReflectConsumedUsage_AndBlockOnceTheLimitIsReached()
    {
        // Arrange: Free allows 5 links a month
        var user = await Data.UserAsync();
        await Seed(async db =>
        {
            var loaded = await db.Set<User>().Include(u => u.Usage).SingleAsync(u => u.Id == user.Id);
            for (var i = 0; i < 5; i++) loaded.Usage.IncrementLinksCreated(TestData.Now);
        });
        As(user.Id);
 
        // Act
        var result = await Send(new GetMyQuotaStatusQuery());
 
        // Assert
        result.Value.MonthlyLinksCreated.Should().Be(5);
        result.Value.RemainingLinks.Should().Be(0);
        result.Value.CanCreateMoreLinks.Should().BeFalse();
    }
 
    [Fact]
    public async Task GetMyQuotaStatus_Should_ReportUnlimitedQuota_ForAnEnterpriseUser()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        As(user.Id);
 
        // Act
        var result = await Send(new GetMyQuotaStatusQuery());
 
        // Assert
        result.Value.MonthlyLinksLimit.Should().Be(-1);
        result.Value.RemainingLinks.Should().Be(int.MaxValue);
        result.Value.CanCreateMoreLinks.Should().BeTrue();
    }
 
    [Fact]
    public async Task GetMyQuotaStatus_Should_ReturnNotFound_WhenTheUsageRowIsGone()
    {
        // Arrange: this query reads straight from user_usage keyed by user id, with no is_deleted filter,
        // so a soft delete alone would not reproduce a missing row — only removing it does.
        var user = await Data.UserAsync();
        As(user.Id);
        await Seed(db => db.Set<User>().Where(u => u.Id == user.Id).ExecuteDeleteAsync());
 
        // Act
        var result = await Send(new GetMyQuotaStatusQuery());
 
        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
    }
}
