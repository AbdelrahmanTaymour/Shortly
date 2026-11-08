using Microsoft.EntityFrameworkCore;
using Shortly.Core.Exceptions;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Persistence;

public class UserRepositoryTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private async Task SoftDelete(User user)
    {
        var deletedBy = await Data.UserAsync(); // deleted_by is a foreign key to users
        await Seed(async db => (await db.Set<User>().SingleAsync(u => u.Id == user.Id)).SoftDelete(deletedBy.Id, TestData.Now));
    }

    private Task<UserSecurity> Security(Guid userId) => Query(db => db.Set<UserSecurity>().SingleAsync(s => s.UserId == userId));

    // ---- reads --------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_Should_LoadTheProfileSecurityAndUsage()
    {
        // Arrange
        var user = await Data.UserAsync();

        // Act
        var loaded = await WithRepository<IUserRepository, User?>(r => r.GetByIdAsync(user.Id), save: false);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Profile.Should().NotBeNull();
        loaded.Security.Should().NotBeNull();
        loaded.Usage.Should().NotBeNull();
        loaded.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnNull_ForAnUnknownId()
    {
        // Act
        var loaded = await WithRepository<IUserRepository, User?>(r => r.GetByIdAsync(Guid.NewGuid()), save: false);

        // Assert
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task EveryLookup_Should_HideASoftDeletedUser()
    {
        // Arrange
        var user = await Data.UserAsync();
        await SoftDelete(user);

        // Act
        var byId = await WithRepository<IUserRepository, User?>(r => r.GetByIdAsync(user.Id), save: false);
        var byEmail = await WithRepository<IUserRepository, User?>(r => r.GetByEmailAsync(user.Email), save: false);
        var byUsername = await WithRepository<IUserRepository, User?>(r => r.GetByUsernameAsync(user.Username), save: false);
        var byEither = await WithRepository<IUserRepository, User?>(r => r.GetByEmailOrUsernameAsync(user.Email), save: false);
        var byIds = await WithRepository<IUserRepository, IReadOnlyCollection<User>>(r => r.GetByIdsAsync([user.Id]), save: false);

        // Assert
        byId.Should().BeNull();
        byEmail.Should().BeNull();
        byUsername.Should().BeNull();
        byEither.Should().BeNull();
        byIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByEmailOrUsernameAsync_Should_MatchEitherField()
    {
        // Arrange
        var user = await Data.UserAsync();

        // Act
        var byEmail = await WithRepository<IUserRepository, User?>(r => r.GetByEmailOrUsernameAsync(user.Email), save: false);
        var byUsername = await WithRepository<IUserRepository, User?>(r => r.GetByEmailOrUsernameAsync(user.Username), save: false);

        // Assert
        byEmail!.Id.Should().Be(user.Id);
        byUsername!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByGoogleIdAsync_Should_FindTheLinkedUser()
    {
        // Arrange
        var user = await Data.UserAsync();
        var googleId = $"g-{Guid.NewGuid():N}";
        await Seed(async db => (await db.Set<User>().SingleAsync(u => u.Id == user.Id)).LinkGoogleAccount(googleId, null, TestData.Now));

        // Act
        var found = await WithRepository<IUserRepository, User?>(r => r.GetByGoogleIdAsync(googleId), save: false);

        // Assert
        found!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByIdsAsync_Should_ReturnOnlyExistingUsers_AndNothingForAnEmptyList()
    {
        // Arrange
        var a = await Data.UserAsync();
        var b = await Data.UserAsync();

        // Act
        var found = await WithRepository<IUserRepository, IReadOnlyCollection<User>>(
            r => r.GetByIdsAsync([a.Id, b.Id, Guid.NewGuid()]), save: false);
        var none = await WithRepository<IUserRepository, IReadOnlyCollection<User>>(r => r.GetByIdsAsync([]), save: false);

        // Assert
        found.Select(u => u.Id).Should().BeEquivalentTo([a.Id, b.Id]);
        found.Should().OnlyContain(u => u.Profile != null && u.Security != null && u.Usage != null);
        none.Should().BeEmpty();
    }

    // ---- existence checks ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task ExistenceChecks_Should_StillSeeASoftDeletedUser_BecauseTheUniqueIndexDoes()
    {
        // Arrange
        var user = await Data.UserAsync();
        await SoftDelete(user);

        // Act / Assert
        (await WithRepository<IUserRepository, bool>(r => r.ExistsAsync(user.Id), save: false)).Should().BeTrue();
        (await WithRepository<IUserRepository, bool>(r => r.EmailExistsAsync(user.Email), save: false)).Should().BeTrue();
        (await WithRepository<IUserRepository, bool>(r => r.UsernameExistsAsync(user.Username), save: false)).Should().BeTrue();
        (await WithRepository<IUserRepository, bool>(r => r.EmailOrUsernameExistsAsync(user.Email, "other"), save: false)).Should().BeTrue();
        (await WithRepository<IUserRepository, bool>(r => r.EmailOrUsernameExistsAsync("other@example.com", user.Username), save: false)).Should().BeTrue();
    }

    [Fact]
    public async Task ExistenceChecks_Should_BeFalse_ForUnknownValues()
    {
        // Act / Assert
        (await WithRepository<IUserRepository, bool>(r => r.ExistsAsync(Guid.NewGuid()), save: false)).Should().BeFalse();
        (await WithRepository<IUserRepository, bool>(r => r.EmailExistsAsync($"{Guid.NewGuid():N}@nowhere.test"), save: false)).Should().BeFalse();
        (await WithRepository<IUserRepository, bool>(r => r.UsernameExistsAsync(Guid.NewGuid().ToString("N")), save: false)).Should().BeFalse();
    }

    // ---- constraints ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Saving_Should_Fail_ForADuplicateEmail_OrUsername()
    {
        // Arrange
        var existing = await Data.UserAsync();

        // Act
        var sameEmail = () => Data.UserAsync(email: existing.Email);
        var sameUsername = () => Data.UserAsync(username: existing.Username);

        // Assert
        await sameEmail.Should().ThrowAsync<UniqueConstraintViolationException>();
        await sameUsername.Should().ThrowAsync<UniqueConstraintViolationException>();
    }

    // ---- failed-login counting ----------------------------------------------------------------------------------------

    [Fact]
    public async Task RecordFailedLoginAsync_Should_CountEachAttempt_AndLockAtTheMaximum()
    {
        // Arrange
        var user = await Data.UserAsync();
        var now = TestData.Now;

        // Act
        await WithRepository<IUserRepository, bool>(async r => { await r.RecordFailedLoginAsync(user.Id, now, 3, TimeSpan.FromMinutes(15)); return true; }, save: false);
        var afterOne = await Security(user.Id);
        for (var i = 0; i < 2; i++)
            await WithRepository<IUserRepository, bool>(async r => { await r.RecordFailedLoginAsync(user.Id, now, 3, TimeSpan.FromMinutes(15)); return true; }, save: false);
        var afterThree = await Security(user.Id);

        // Assert
        afterOne.FailedLoginAttempts.Should().Be(1);
        afterOne.LockedUntilUtc.Should().BeNull();
        afterThree.FailedLoginAttempts.Should().Be(3);
        afterThree.LockedUntilUtc.Should().BeCloseTo(now.AddMinutes(15), TimeSpan.FromSeconds(1));
        afterThree.LockoutReason.Should().Contain("3 consecutive failed login attempts");
    }

    [Fact]
    public async Task RecordFailedLoginAsync_Should_IgnoreAttemptsDuringAnActiveLockout()
    {
        // Arrange
        var user = await Data.UserAsync();
        var now = TestData.Now;
        for (var i = 0; i < 3; i++)
            await WithRepository<IUserRepository, bool>(async r => { await r.RecordFailedLoginAsync(user.Id, now, 3, TimeSpan.FromMinutes(15)); return true; }, save: false);

        // Act
        await WithRepository<IUserRepository, bool>(async r => { await r.RecordFailedLoginAsync(user.Id, now.AddMinutes(1), 3, TimeSpan.FromMinutes(15)); return true; }, save: false);

        // Assert
        var security = await Security(user.Id);
        security.FailedLoginAttempts.Should().Be(3);
        security.LockedUntilUtc.Should().BeCloseTo(now.AddMinutes(15), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RecordFailedLoginAsync_Should_StartCountingAgain_AfterTheLockoutExpired()
    {
        // Arrange
        var user = await Data.UserAsync();
        var now = TestData.Now;
        for (var i = 0; i < 3; i++)
            await WithRepository<IUserRepository, bool>(async r => { await r.RecordFailedLoginAsync(user.Id, now, 3, TimeSpan.FromMinutes(15)); return true; }, save: false);

        // Act: 16 minutes later the lock is over
        await WithRepository<IUserRepository, bool>(async r => { await r.RecordFailedLoginAsync(user.Id, now.AddMinutes(16), 3, TimeSpan.FromMinutes(15)); return true; }, save: false);

        // Assert
        var security = await Security(user.Id);
        security.FailedLoginAttempts.Should().Be(1);
        security.LockedUntilUtc.Should().BeNull();
        security.LockoutReason.Should().BeNull();
    }

    [Fact]
    public async Task RecordFailedLoginAsync_Should_CountEveryConcurrentAttempt()
    {
        // Arrange
        var user = await Data.UserAsync();
        var now = TestData.Now;

        // Act: ten simultaneous wrong passwords must all be counted (one UPDATE statement, no lost updates)
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => WithRepository<IUserRepository, bool>(
            async r => { await r.RecordFailedLoginAsync(user.Id, now, 100, TimeSpan.FromMinutes(15)); return true; }, save: false)));

        // Assert
        (await Security(user.Id)).FailedLoginAttempts.Should().Be(10);
    }
}
