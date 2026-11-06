using Shortly.Core.Analytics;

namespace Shortly.Core.UnitTests.Analytics;

public class AnalyticsScopeTests
{
    private static readonly DateTime From = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildWhereClause_Should_BeTrue_WhenNothingIsScoped()
    {
        // Act
        var clause = new AnalyticsScope(null, null, null, null, null).BuildWhereClause();

        // Assert
        clause.Should().Be("TRUE");
    }

    [Fact]
    public void BuildWhereClause_Should_FilterOneLink_ByItsId()
    {
        // Act
        var clause = new AnalyticsScope(42, null, null, null, null).BuildWhereClause();

        // Assert
        clause.Should().Be("ce.short_url_id = @ShortUrlId");
    }

    [Fact]
    public void BuildWhereClause_Should_FilterAPersonalWorkspace_ByTheUser()
    {
        // Act
        var clause = new AnalyticsScope(null, Guid.NewGuid(), null, null, null).BuildWhereClause();

        // Assert
        clause.Should().Be("s.user_id = @UserId");
    }

    [Fact]
    public void BuildWhereClause_Should_FilterAnOrganization_ByTheOrganization()
    {
        // Act
        var clause = new AnalyticsScope(null, null, Guid.NewGuid(), null, null).BuildWhereClause();

        // Assert
        clause.Should().Be("s.organization_id = @OrganizationId");
    }

    [Fact]
    public void BuildWhereClause_Should_UseAnInclusiveStart_AndAnExclusiveEnd()
    {
        // Act
        var clause = new AnalyticsScope(null, null, null, From, From.AddDays(1)).BuildWhereClause();

        // Assert
        clause.Should().Be("ce.clicked_at_utc >= @From AND ce.clicked_at_utc < @To");
    }

    [Fact]
    public void BuildWhereClause_Should_CombineEveryGivenFilterWithAnd()
    {
        // Act
        var clause = new AnalyticsScope(7, Guid.NewGuid(), Guid.NewGuid(), From, null).BuildWhereClause();

        // Assert
        clause.Should().Be("ce.short_url_id = @ShortUrlId AND s.user_id = @UserId AND s.organization_id = @OrganizationId AND ce.clicked_at_utc >= @From");
    }

    [Fact]
    public void Scope_Should_ExcludeGeographyAndCampaigns_UnlessGranted()
    {
        // Act
        var scope = new AnalyticsScope(null, null, null, null, null);

        // Assert
        scope.IncludeGeo.Should().BeFalse();
        scope.IncludeCampaigns.Should().BeFalse();
    }
}
