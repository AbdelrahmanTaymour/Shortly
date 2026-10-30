using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Users.GetLoggedInUser;

internal sealed class GetLoggedInUserQueryHandler(
    IUserContext userContext, 
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetLoggedInUserQuery, LoggedInUserResponse>
{
    public async Task<Result<LoggedInUserResponse>> Handle(GetLoggedInUserQuery request, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT u.id AS Id, u.email AS Email, u.username AS Username, u.role AS Role,
                                  p.profile_picture_url AS ProfilePictureUrl, p.name AS Name,
                                  u.subscription_plan_id AS SubscriptionPlanId, u.is_email_confirmed AS IsEmailConfirmed
                           FROM users u
                           JOIN user_profile p ON p.user_id = u.id
                           WHERE u.id = @UserId
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        var result = await connection.QuerySingleOrDefaultAsync<LoggedInUserResponse>(sql, new { userContext.UserId });
        
        return result ?? Result.Failure<LoggedInUserResponse>(UserErrors.NotFound);
    }
}