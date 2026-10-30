using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Users.GetMyQuotaStatus;

internal sealed class GetMyQuotaStatusQueryHandler(
    IUserContext userContext, 
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetMyQuotaStatusQuery, QuotaStatusResponse>
{
    public async Task<Result<QuotaStatusResponse>> Handle(GetMyQuotaStatusQuery request, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT uu.monthly_links_created AS MonthlyLinksCreated, sp.max_links_per_month AS MonthlyLinksLimit,
                   uu.monthly_qr_codes_created AS MonthlyQrCodesCreated, sp.max_qr_codes_per_month AS MonthlyQrCodesLimit,
                   uu.monthly_reset_date AS MonthlyResetDateUtc
            FROM user_usage uu
            JOIN users u ON u.id = uu.user_id
            JOIN subscription_plans sp ON sp.id = u.subscription_plan_id
            WHERE uu.user_id = @UserId
            """;

        using var connection = sqlConnectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<QuotaRow>(sql, new { userContext.UserId });
        if (row is null) return Result.Failure<QuotaStatusResponse>(UserErrors.NotFound);

        return new QuotaStatusResponse(
            row.MonthlyLinksCreated, row.MonthlyLinksLimit,
            row.MonthlyLinksLimit < 0 ? int.MaxValue : Math.Max(0, row.MonthlyLinksLimit - row.MonthlyLinksCreated),
            row.MonthlyLinksLimit < 0 || row.MonthlyLinksCreated < row.MonthlyLinksLimit,
            row.MonthlyQrCodesCreated, row.MonthlyQrCodesLimit,
            row.MonthlyQrCodesLimit < 0 ? int.MaxValue : Math.Max(0, row.MonthlyQrCodesLimit - row.MonthlyQrCodesCreated),
            row.MonthlyQrCodesLimit < 0 || row.MonthlyQrCodesCreated < row.MonthlyQrCodesLimit,
            row.MonthlyResetDateUtc);
    }

    private sealed class QuotaRow
    {
        public int MonthlyLinksCreated { get; init; }
        public int MonthlyLinksLimit { get; init; }
        public int MonthlyQrCodesCreated { get; init; }
        public int MonthlyQrCodesLimit { get; init; }
        public DateTime MonthlyResetDateUtc { get; init; }
    }
}