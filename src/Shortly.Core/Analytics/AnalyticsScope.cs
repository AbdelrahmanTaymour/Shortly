namespace Shortly.Core.Analytics;

internal readonly record struct AnalyticsScope(long? ShortUrlId, Guid? UserId, Guid? OrganizationId, DateTime? From, DateTime? To)
{
    public string BuildWhereClause()
    {
        var clauses = new List<string> { "1 = 1" };

        if (ShortUrlId is not null) clauses.Add("ce.short_url_id = @ShortUrlId");
        if (UserId is not null) clauses.Add("s.user_id = @UserId");
        if (OrganizationId is not null) clauses.Add("s.organization_id = @OrganizationId");
        if (From is not null) clauses.Add("ce.clicked_at >= @From");
        if (To is not null) clauses.Add("ce.clicked_at <= @To");

        return string.Join(" AND ", clauses);
    }
}