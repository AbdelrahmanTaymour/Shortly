using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.Users.GetMyProfile;

internal sealed class GetMyProfileQueryHandler(IUserContext userContext, ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetMyProfileQuery, MyProfileResponse>
{
    public async Task<Result<MyProfileResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT u.id AS Id, u.email AS Email, u.username AS Username, u.is_email_confirmed AS IsEmailConfirmed,
                                  u.created_at AS CreatedAtUtc, p.name AS Name, p.bio AS Bio, p.phone_number AS PhoneNumber,
                                  p.profile_picture_url AS ProfilePictureUrl, p.website AS Website, p.company AS Company,
                                  p.location AS Location, p.country AS Country, p.time_zone AS TimeZone
                           FROM users u JOIN user_profile p ON p.user_id = u.id
                           WHERE u.id = @UserId
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        var result = await connection.QuerySingleOrDefaultAsync<MyProfileResponse>(sql, new { userContext.UserId });
        return result ?? Result.Failure<MyProfileResponse>(UserErrors.NotFound);
    }
}