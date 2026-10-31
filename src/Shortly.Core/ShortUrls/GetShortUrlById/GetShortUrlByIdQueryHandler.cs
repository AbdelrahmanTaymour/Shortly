using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.GetShortUrlById;

internal sealed class GetShortUrlByIdQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetShortUrlByIdQuery, ShortUrlResponse>
{
    public async Task<Result<ShortUrlResponse>> Handle(GetShortUrlByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT id AS Id, original_url AS OriginalUrl, short_code AS ShortCode, is_active AS IsActive,
                                  total_clicks AS TotalClicks, click_limit AS ClickLimit, is_private AS IsPrivate,
                                  is_password_protected AS IsPasswordProtected, expires_at AS ExpiresAt,
                                  title AS Title, description AS Description, created_at_utc AS CreatedAtUtc
                           FROM short_urls
                           WHERE id = @ShortUrlId
                             AND (user_id = @UserId OR organization_id = @OrganizationId OR @IsAdmin = TRUE)
                           """;

        var response = await connection.QueryFirstOrDefaultAsync<ShortUrlResponse>(sql, new
        {
            request.ShortUrlId,
            userContext.UserId,
            userContext.OrganizationId,
            userContext.IsAdmin
        });

        return response ?? Result.Failure<ShortUrlResponse>(ShortUrlErrors.NotFound);
    }
}