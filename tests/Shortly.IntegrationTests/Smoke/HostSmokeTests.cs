using Microsoft.EntityFrameworkCore;
using Shortly.Core.Users.GetMyQuotaStatus;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;
using PlanEntity = Shortly.Domain.SubscriptionPlan.SubscriptionPlan;

namespace Shortly.IntegrationTests.Smoke;

public class HostSmokeTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Migrations_Should_SeedTheFourSubscriptionPlans()
    {
        // Act
        var names = await Query(db => db.Set<PlanEntity>().OrderBy(p => p.Id).Select(p => p.Name).ToListAsync());

        // Assert
        names.Should().Equal("Free", "Starter", "Professional", "Enterprise");
    }

    [Fact]
    public async Task Send_Should_RunTheRealPipelineAndHandler_AgainstTheRealDatabase()
    {
        // Arrange
        var user = User.Register($"{Guid.NewGuid():N}@example.com", Guid.NewGuid().ToString("N"), "hash", DateTime.UtcNow).Value;
        await Seed(db => { db.Add(user); return Task.CompletedTask; });
        As(user.Id);

        // Act
        var result = await Send(new GetMyQuotaStatusQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MonthlyLinksCreated.Should().Be(0);
        result.Value.MonthlyLinksLimit.Should().Be(await Query(db =>
            db.Set<PlanEntity>().Where(p => p.Id == user.SubscriptionPlanId).Select(p => p.MaxLinksPerMonth).SingleAsync()));
    }

    [Fact]
    public async Task Send_Should_ActAsTheChosenCaller_AndNotAsAnyoneElse()
    {
        // Arrange: a caller that exists nowhere in the database
        As(Guid.NewGuid());

        // Act
        var result = await Send(new GetMyQuotaStatusQuery());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
    }
}
