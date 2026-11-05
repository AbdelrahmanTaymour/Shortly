using Shortly.Domain.Abstractions;

namespace Shortly.Domain.UnitTests.Abstractions;

public class EntityTests
{
    private sealed class GuidEntity(Guid id) : Entity(id);

    private sealed class OtherGuidEntity(Guid id) : Entity(id);

    private sealed class LongEntity(long id) : Entity<long>(id);

    private sealed class OtherLongEntity(long id) : Entity<long>(id);

    // ---- identity ----------------------------------------------------------------------------------------------------

    [Fact]
    public void Entity_Should_ExposeTheIdItWasCreatedWith()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new GuidEntity(id);

        // Assert
        entity.Id.Should().Be(id);
    }

    [Fact]
    public void GenericEntity_Should_ExposeTheIdItWasCreatedWith()
    {
        // Act
        var entity = new LongEntity(42);

        // Assert
        entity.Id.Should().Be(42);
    }

    // ---- Guid entities -----------------------------------------------------------------------------------------------

    [Fact]
    public void Entities_Should_BeEqual_WhenTheyHaveTheSameTypeAndId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var first = new GuidEntity(id);
        var second = new GuidEntity(id);

        // Assert
        first.Equals(second).Should().BeTrue();
        first.Equals((object)second).Should().BeTrue();
        (first == second).Should().BeTrue();
        (first != second).Should().BeFalse();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Entities_Should_NotBeEqual_WhenTheirIdsDiffer()
    {
        // Arrange
        var first = new GuidEntity(Guid.NewGuid());
        var second = new GuidEntity(Guid.NewGuid());

        // Assert
        first.Equals(second).Should().BeFalse();
        (first == second).Should().BeFalse();
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void Entities_Should_NotBeEqual_WhenTheyShareAnIdButHaveDifferentTypes()
    {
        // Arrange
        var id = Guid.NewGuid();
        var first = new GuidEntity(id);
        var second = new OtherGuidEntity(id);

        // Assert
        first.Equals(second).Should().BeFalse();
        (first == second).Should().BeFalse();
    }

    [Fact]
    public void Entity_Should_NeverEqualNull()
    {
        // Arrange
        var entity = new GuidEntity(Guid.NewGuid());

        // Assert
        entity.Equals(null).Should().BeFalse();
        entity.Equals((object?)null).Should().BeFalse();
        (entity == null).Should().BeFalse();
        (entity != null).Should().BeTrue();
        ((GuidEntity?)null == (GuidEntity?)null).Should().BeTrue();
    }

    [Fact]
    public void Entity_Should_NotEqualAnObjectOfAnotherKind()
    {
        // Arrange
        var entity = new GuidEntity(Guid.NewGuid());

        // Assert
        entity.Equals("not an entity").Should().BeFalse();
    }

    [Fact]
    public void AnEntityWithoutAnIdentity_Should_OnlyEqualItself()
    {
        // Arrange
        var first = new GuidEntity(Guid.Empty);
        var second = new GuidEntity(Guid.Empty);

        // Assert
        first.Equals(first).Should().BeTrue();
        first.Equals(second).Should().BeFalse();
        (first == second).Should().BeFalse();
    }

    [Fact]
    public void Entities_Should_BeDeduplicatedByIdentityInASet()
    {
        // Arrange
        var id = Guid.NewGuid();
        var set = new HashSet<Entity> { new GuidEntity(id), new GuidEntity(id), new GuidEntity(Guid.NewGuid()) };

        // Assert
        set.Should().HaveCount(2);
        set.Contains(new GuidEntity(id)).Should().BeTrue();
    }

    // ---- generic entities --------------------------------------------------------------------------------------------

    [Fact]
    public void GenericEntities_Should_BeEqual_WhenTheyHaveTheSameTypeAndId()
    {
        // Arrange
        var first = new LongEntity(7);
        var second = new LongEntity(7);

        // Assert
        first.Equals(second).Should().BeTrue();
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void GenericEntities_Should_NotBeEqual_WhenTheirIdsDiffer()
    {
        // Assert
        (new LongEntity(1) == new LongEntity(2)).Should().BeFalse();
        (new LongEntity(1) != new LongEntity(2)).Should().BeTrue();
    }

    [Fact]
    public void GenericEntities_Should_NotBeEqual_WhenTheyShareAnIdButHaveDifferentTypes()
    {
        // Assert
        new LongEntity(7).Equals(new OtherLongEntity(7)).Should().BeFalse();
    }

    [Fact]
    public void AGenericEntityThatWasNotSavedYet_Should_OnlyEqualItself()
    {
        // Arrange: a database-generated key is still 0 before the first save
        var first = new LongEntity(0);
        var second = new LongEntity(0);

        // Assert
        first.Equals(first).Should().BeTrue();
        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void GenericEntity_Should_NeverEqualNull()
    {
        // Arrange
        var entity = new LongEntity(7);

        // Assert
        entity.Equals(null).Should().BeFalse();
        (entity == null).Should().BeFalse();
        (entity != null).Should().BeTrue();
    }

    [Fact]
    public void GenericEntities_Should_BeDeduplicatedByIdentityInASet()
    {
        // Arrange
        var set = new HashSet<Entity<long>> { new LongEntity(1), new LongEntity(1), new LongEntity(2) };

        // Assert
        set.Should().HaveCount(2);
    }
}
