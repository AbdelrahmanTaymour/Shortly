using Microsoft.EntityFrameworkCore;
using Shortly.Core.Exceptions;
using Shortly.Domain.Members;
using Shortly.Domain.Teams;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Persistence;

public class TeamRepositoryTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private async Task<(TestData.OrgFixture Org, OrganizationMember Manager, OrganizationTeam Team)> TeamAsync(string? name = null)
    {
        var org = await Data.OrganizationAsync();
        var (_, manager) = await Data.MemberAsync(org.Id, MemberRole.TeamManager);
        return (org, manager, await Data.TeamAsync(org.Id, manager.Id, name));
    }

    // ---- name uniqueness check ---------------------------------------------------------------------------------------

    [Fact]
    public async Task IsTeamNameExistAsync_Should_FindANameInTheSameOrganization_Only()
    {
        // Arrange
        var (org, _, team) = await TeamAsync("Marketing");
        var other = await Data.OrganizationAsync();

        // Act / Assert
        (await WithRepository<IOrganizationTeamRepository, bool>(r => r.IsTeamNameExistAsync("Marketing", org.Id), save: false)).Should().BeTrue();
        (await WithRepository<IOrganizationTeamRepository, bool>(r => r.IsTeamNameExistAsync("Marketing", other.Id), save: false)).Should().BeFalse();
        (await WithRepository<IOrganizationTeamRepository, bool>(r => r.IsTeamNameExistAsync("Sales", org.Id), save: false)).Should().BeFalse();
    }

    [Fact]
    public async Task IsTeamNameExistAsync_Should_IgnoreTheExcludedTeam()
    {
        // Arrange
        var (org, _, team) = await TeamAsync("Marketing");

        // Act / Assert
        (await WithRepository<IOrganizationTeamRepository, bool>(r => r.IsTeamNameExistAsync("Marketing", org.Id, team.Id), save: false)).Should().BeFalse();
        (await WithRepository<IOrganizationTeamRepository, bool>(r => r.IsTeamNameExistAsync("Marketing", org.Id, Guid.NewGuid()), save: false)).Should().BeTrue();
    }

    [Fact]
    public async Task Saving_Should_Fail_ForTwoTeamsWithTheSameNameInOneOrganization()
    {
        // Arrange
        var (org, manager, _) = await TeamAsync("Marketing");

        // Act
        var duplicate = () => Data.TeamAsync(org.Id, manager.Id, "Marketing");

        // Assert
        await duplicate.Should().ThrowAsync<UniqueConstraintViolationException>();
    }

    // ---- manager checks ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task IsManagerOfAnyTeamAsync_Should_BeTrueOnlyForAnAssignedManager()
    {
        // Arrange
        var (org, manager, _) = await TeamAsync();
        var (_, plain) = await Data.MemberAsync(org.Id);

        // Act / Assert
        (await WithRepository<IOrganizationTeamRepository, bool>(r => r.IsManagerOfAnyTeamAsync(manager.Id), save: false)).Should().BeTrue();
        (await WithRepository<IOrganizationTeamRepository, bool>(r => r.IsManagerOfAnyTeamAsync(plain.Id), save: false)).Should().BeFalse();
    }

    [Fact]
    public async Task IsTeamManagerAsync_Should_MatchTheTeamAndManagerTogether()
    {
        // Arrange
        var (org, manager, team) = await TeamAsync();
        var otherTeam = await Data.TeamAsync(org.Id, manager.Id);
        var (_, other) = await Data.MemberAsync(org.Id, MemberRole.TeamManager);

        // Act / Assert
        (await WithRepository<IOrganizationTeamRepository, bool>(r => r.IsTeamManagerAsync(team.Id, manager.Id), save: false)).Should().BeTrue();
        (await WithRepository<IOrganizationTeamRepository, bool>(r => r.IsTeamManagerAsync(team.Id, other.Id), save: false)).Should().BeFalse();
        (await WithRepository<IOrganizationTeamRepository, bool>(r => r.IsTeamManagerAsync(otherTeam.Id, other.Id), save: false)).Should().BeFalse();
    }

    // ---- team members --------------------------------------------------------------------------------------------------

    [Fact]
    public async Task TeamMemberLookups_Should_FindMembershipsByTeamAndByMember()
    {
        // Arrange
        var (org, manager, team) = await TeamAsync();
        var (_, member) = await Data.MemberAsync(org.Id);
        await Data.TeamMemberAsync(team.Id, member.Id);
        var secondTeam = await Data.TeamAsync(org.Id, manager.Id);
        await Data.TeamMemberAsync(secondTeam.Id, member.Id);

        // Act
        var one = await WithRepository<IOrganizationTeamMemberRepository, OrganizationTeamMember?>(
            r => r.GetByTeamAndMemberAsync(team.Id, member.Id), save: false);
        var all = await WithRepository<IOrganizationTeamMemberRepository, IReadOnlyList<OrganizationTeamMember>>(
            r => r.GetByMemberAsync(member.Id), save: false);
        var isMember = await WithRepository<IOrganizationTeamMemberRepository, bool>(r => r.IsMemberOfTeamAsync(member.Id, team.Id), save: false);
        var notMember = await WithRepository<IOrganizationTeamMemberRepository, bool>(r => r.IsMemberOfTeamAsync(Guid.NewGuid(), team.Id), save: false);

        // Assert
        one.Should().NotBeNull();
        all.Select(tm => tm.TeamId).Should().BeEquivalentTo([team.Id, secondTeam.Id]);
        isMember.Should().BeTrue();
        notMember.Should().BeFalse();
    }

    [Fact]
    public async Task Saving_Should_Fail_ForTheSameMemberTwiceOnOneTeam()
    {
        // Arrange
        var (_, manager, team) = await TeamAsync(); // the manager is already on the team

        // Act
        var duplicate = () => Data.TeamMemberAsync(team.Id, manager.Id);

        // Assert
        await duplicate.Should().ThrowAsync<UniqueConstraintViolationException>();
    }

    [Fact]
    public async Task DeletingATeam_Should_RemoveItsMemberships_ByCascade()
    {
        // Arrange
        var (org, manager, team) = await TeamAsync();
        var (_, member) = await Data.MemberAsync(org.Id);
        await Data.TeamMemberAsync(team.Id, member.Id);

        // Act
        await WithRepository<IOrganizationTeamRepository, bool>(async r =>
        {
            r.Remove((await r.GetByIdAsync(team.Id))!);
            return true;
        });

        // Assert
        (await Query(db => db.Set<OrganizationTeamMember>().CountAsync(tm => tm.TeamId == team.Id))).Should().Be(0);
    }
}
