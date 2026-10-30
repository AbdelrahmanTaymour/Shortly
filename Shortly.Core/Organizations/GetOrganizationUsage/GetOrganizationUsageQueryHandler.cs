using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;

namespace Shortly.Core.Organizations.GetOrganizationUsage;

internal sealed class GetOrganizationUsageQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetOrganizationUsageQuery, OrganizationUsageResponse>
{
    public async Task<Result<OrganizationUsageResponse>> Handle(GetOrganizationUsageQuery request, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT ou.monthly_links_created AS MonthlyLinksCreated, sp.max_links_per_month AS MonthlyLinksLimit,
                   ou.monthly_qr_codes_created AS MonthlyQrCodesCreated, sp.max_qr_codes_per_month AS MonthlyQrCodesLimit,
                   ou.monthly_reset_date AS MonthlyResetDateUtc
            FROM organization_usage ou
            JOIN organizations o ON o.id = ou.organization_id
            JOIN subscription_plans sp ON sp.id = o.subscription_plan_id
            WHERE ou.organization_id = @OrganizationId AND o.is_active = TRUE AND o.deleted_at IS NULL
            """;

        using var connection = sqlConnectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<UsageRow>(sql, new { request.OrganizationId });
        if (row is null) return Result.Failure<OrganizationUsageResponse>(OrganizationErrors.NotFound);

        return new OrganizationUsageResponse(
            row.MonthlyLinksCreated, row.MonthlyLinksLimit,
            row.MonthlyLinksLimit < 0 ? int.MaxValue : Math.Max(0, row.MonthlyLinksLimit - row.MonthlyLinksCreated),
            row.MonthlyLinksLimit < 0 || row.MonthlyLinksCreated < row.MonthlyLinksLimit,
            row.MonthlyQrCodesCreated, row.MonthlyQrCodesLimit,
            row.MonthlyQrCodesLimit < 0 ? int.MaxValue : Math.Max(0, row.MonthlyQrCodesLimit - row.MonthlyQrCodesCreated),
            row.MonthlyQrCodesLimit < 0 || row.MonthlyQrCodesCreated < row.MonthlyQrCodesLimit,
            row.MonthlyResetDateUtc);
    }

    private sealed class UsageRow
    {
        public int MonthlyLinksCreated { get; init; }
        public int MonthlyLinksLimit { get; init; }
        public int MonthlyQrCodesCreated { get; init; }
        public int MonthlyQrCodesLimit { get; init; }
        public DateTime MonthlyResetDateUtc { get; init; }
    }
}