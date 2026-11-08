using Microsoft.EntityFrameworkCore;
using Shortly.Core.Admin.HardDeleteUser;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Admin;

public class HardDeleteUserTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    // ---- success ----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task HardDeleteUser_Should_RemoveTheUser_AndEveryPersonalLink()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(Caller.Admin());

        // Act
        var result = await Send(new HardDeleteUserCommand(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Query(db => db.Set<User>().AnyAsync(u => u.Id == user.Id))).Should().BeFalse();
        (await Query(db => db.Set<ShortUrl>().AnyAsync(s => s.Id == link.Id))).Should().BeFalse();
    }

    // ---- guards -------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task HardDeleteUser_Should_RefuseToDeleteTheCallersOwnAccount()
    {
        // Arrange
        var admin = await Data.UserAsync();
        As(admin.Id);

        // Act
        var result = await Send(new HardDeleteUserCommand(admin.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.CannotTargetSelf);
        (await Query(db => db.Set<User>().AnyAsync(u => u.Id == admin.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task HardDeleteUser_Should_RefuseToDeleteAUserThatStillOwnsAnOrganization()
    {
        // Arrange
        var owner = (await Data.OrganizationAsync()).Owner;
        As(Caller.Admin());

        // Act
        var result = await Send(new HardDeleteUserCommand(owner.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.OwnsOrganizations);
        (await Query(db => db.Set<User>().AnyAsync(u => u.Id == owner.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task HardDeleteUser_Should_Fail_ForAUserThatDoesNotExist()
    {
        // Arrange
        As(Caller.Admin());

        // Act
        var result = await Send(new HardDeleteUserCommand(Guid.NewGuid()));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
    }
}