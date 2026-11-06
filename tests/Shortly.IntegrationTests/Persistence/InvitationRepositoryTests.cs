using Microsoft.EntityFrameworkCore;
using Shortly.Core.Exceptions;
using Shortly.Domain.Invitations;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Persistence;

public class InvitationRepositoryTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetByTokenHashAsync_Should_FindTheInvitation_ByItsHash()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var invitation = await Data.InvitationAsync(org.Id, tokenHash: $"hash-{Guid.NewGuid():N}");

        // Act
        var found = await WithRepository<IOrganizationInvitationRepository, OrganizationInvitation?>(r => r.GetByTokenHashAsync(invitation.TokenHash), save: false);
        var missing = await WithRepository<IOrganizationInvitationRepository, OrganizationInvitation?>(r => r.GetByTokenHashAsync("nope"), save: false);

        // Assert
        found!.Id.Should().Be(invitation.Id);
        missing.Should().BeNull();
    }

    [Fact]
    public async Task HasPendingInvitationAsync_Should_BeTrue_OnlyForAPendingUnexpiredInvitationOfThatEmailAndOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var other = await Data.OrganizationAsync();
        var invitation = await Data.InvitationAsync(org.Id, "pending@example.com");

        // Act
        Task<bool> Has(string email, Guid orgId, DateTime at) =>
            WithRepository<IOrganizationInvitationRepository, bool>(r => r.HasPendingInvitationAsync(email, orgId, at), save: false);

        // Assert
        (await Has("pending@example.com", org.Id, TestData.Now)).Should().BeTrue();
        (await Has("pending@example.com", other.Id, TestData.Now)).Should().BeFalse();
        (await Has("someone@example.com", org.Id, TestData.Now)).Should().BeFalse();
        (await Has("pending@example.com", org.Id, invitation.ExpiresAt)).Should().BeFalse();
        (await Has("pending@example.com", org.Id, invitation.ExpiresAt.AddMinutes(-1))).Should().BeTrue();
    }

    [Fact]
    public async Task HasPendingInvitationAsync_Should_BeFalse_ForAnAcceptedInvitation()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var invitation = await Data.InvitationAsync(org.Id, "accepted@example.com");
        await Seed(async db => (await db.Set<OrganizationInvitation>().SingleAsync(i => i.Id == invitation.Id)).Accept(TestData.Now));

        // Act
        var has = await WithRepository<IOrganizationInvitationRepository, bool>(
            r => r.HasPendingInvitationAsync("accepted@example.com", org.Id, TestData.Now), save: false);

        // Assert
        has.Should().BeFalse();
    }

    [Fact]
    public async Task MarkOverdueInvitationsExpiredAsync_Should_ExpireOnlyPendingInvitationsPastTheirDeadline()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var overdue = await Data.InvitationAsync(org.Id, createdAt: TestData.Now.AddDays(-10), expiresAt: TestData.Now.AddDays(-3));
        var live = await Data.InvitationAsync(org.Id);

        // Act
        var changed = await WithRepository<IOrganizationInvitationRepository, int>(r => r.MarkOverdueInvitationsExpiredAsync(TestData.Now), save: false);

        // Assert
        changed.Should().BeGreaterThanOrEqualTo(1);
        (await Query(db => db.Set<OrganizationInvitation>().Where(i => i.Id == overdue.Id).Select(i => i.Status).SingleAsync()))
            .Should().Be(enInvitationStatus.Expired);
        (await Query(db => db.Set<OrganizationInvitation>().Where(i => i.Id == live.Id).Select(i => i.Status).SingleAsync()))
            .Should().Be(enInvitationStatus.Pending);
    }

    [Fact]
    public async Task MarkOverdueInvitationsExpiredAsync_Should_NotTouchAnAcceptedInvitation_EvenPastItsDeadline()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var past = TestData.Now.AddDays(-10);
        var accepted = await Data.InvitationAsync(org.Id, createdAt: past, expiresAt: past.AddDays(1));
        await Seed(async db => (await db.Set<OrganizationInvitation>().SingleAsync(i => i.Id == accepted.Id)).Accept(past.AddHours(1)));

        // Act
        await WithRepository<IOrganizationInvitationRepository, int>(r => r.MarkOverdueInvitationsExpiredAsync(TestData.Now), save: false);

        // Assert
        (await Query(db => db.Set<OrganizationInvitation>().Where(i => i.Id == accepted.Id).Select(i => i.Status).SingleAsync()))
            .Should().Be(enInvitationStatus.Accepted);
    }

    [Fact]
    public async Task Saving_Should_Fail_ForADuplicateTokenHash()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var first = await Data.InvitationAsync(org.Id);

        // Act
        var duplicate = () => Data.InvitationAsync(org.Id, tokenHash: first.TokenHash);

        // Assert
        await duplicate.Should().ThrowAsync<UniqueConstraintViolationException>();
    }
}
