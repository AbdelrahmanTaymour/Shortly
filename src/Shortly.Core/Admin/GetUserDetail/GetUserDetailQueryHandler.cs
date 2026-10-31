using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Admin.GetUserDetail;

internal sealed class GetUserDetailQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetUserDetailQuery, UserAdminDetailResponse>
{
    public async Task<Result<UserAdminDetailResponse>> Handle(GetUserDetailQuery request, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT u.id AS Id, u.email AS Email, u.username AS Username, u.role AS Role,
                   u.subscription_plan_id AS SubscriptionPlanId, u.is_active AS IsActive,
                   u.is_email_confirmed AS IsEmailConfirmed, u.is_o_auth_user AS IsOAuthUser,
                   u.created_at AS CreatedAtUtc, u.updated_at AS UpdatedAtUtc, u.last_login_at AS LastLoginAtUtc,
                   u.is_deleted AS IsDeleted, u.deleted_at AS DeletedAtUtc,
                   p.name AS ProfileName, p.bio AS ProfileBio, p.phone_number AS ProfilePhoneNumber,
                   p.company AS ProfileCompany, p.location AS ProfileLocation, p.country AS ProfileCountry,
                   s.failed_login_attempts AS FailedLoginAttempts,
                   (s.locked_until IS NOT NULL AND s.locked_until > NOW()) AS IsLockedOut,
                   s.locked_until AS LockedUntilUtc, s.two_factor_enabled AS TwoFactorEnabled,
                   uu.monthly_links_created AS MonthlyLinksCreated, uu.total_links_created AS TotalLinksCreated,
                   uu.monthly_qr_codes_created AS MonthlyQrCodesCreated, uu.total_qr_codes_created AS TotalQrCodesCreated
            FROM users u
            JOIN user_profile p ON p.user_id = u.id
            JOIN user_security s ON s.user_id = u.id
            JOIN user_usage uu ON uu.user_id = u.id
            WHERE (@UserId::uuid IS NOT NULL AND u.id = @UserId)
               OR (@Email::text IS NOT NULL AND u.email = @Email)
               OR (@Username::text IS NOT NULL AND u.username = @Username)
            """;

        using var connection = sqlConnectionFactory.CreateConnection();
        var result = await connection.QuerySingleOrDefaultAsync<UserAdminDetailResponse>(sql, request);

        return result ?? Result.Failure<UserAdminDetailResponse>(UserErrors.NotFound);
    }
}