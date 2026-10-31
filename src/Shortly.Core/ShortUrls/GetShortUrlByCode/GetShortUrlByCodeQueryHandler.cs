using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.ShortUrls.GetShortUrlById;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.GetShortUrlByCode;

internal sealed class GetShortUrlByCodeQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetShortUrlByCodeQuery, ShortUrlResponse>
{
    public async Task<Result<ShortUrlResponse>> Handle(GetShortUrlByCodeQuery request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT id AS Id, original_url AS OriginalUrl, short_code AS ShortCode, is_active AS IsActive,
                                  total_clicks AS TotalClicks, click_limit AS ClickLimit, is_private AS IsPrivate,
                                  is_password_protected AS IsPasswordProtected, expires_at AS ExpiresAt,
                                  title AS Title, description AS Description, created_at_utc AS CreatedAtUtc
                           FROM short_urls
                           WHERE short_code = @ShortCode
                             AND (user_id = @UserId OR organization_id = @OrganizationId OR @IsAdmin = TRUE)
                           """;

        var response = await connection.QueryFirstOrDefaultAsync<ShortUrlResponse>(sql, new
        {
            request.ShortCode,
            userContext.UserId,
            userContext.OrganizationId,
            userContext.IsAdmin
        });

        return response ?? Result.Failure<ShortUrlResponse>(ShortUrlErrors.NotFound);
    }
}