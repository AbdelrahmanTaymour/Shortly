using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.RedirectToShortUrl;

internal sealed class RedirectToShortUrlCommandHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RedirectToShortUrlCommand, string>
{
    public async Task<Result<string>> Handle(RedirectToShortUrlCommand request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string selectSql = """
            SELECT id AS Id, original_url AS OriginalUrl, is_active AS IsActive,
                   is_password_protected AS IsPasswordProtected, password_hash AS PasswordHash,
                   expires_at AS ExpiresAt, click_limit AS ClickLimit, total_clicks AS TotalClicks
            FROM short_urls
            WHERE short_code = @ShortCode
            """;

        var row = await connection.QuerySingleOrDefaultAsync<RedirectRow>(selectSql, new { request.ShortCode });

        if (row is null)
            return Result.Failure<string>(ShortUrlErrors.NotFound);

        if (!row.IsActive)
            return Result.Failure<string>(ShortUrlErrors.Inactive);

        if (row.ExpiresAt is not null && row.ExpiresAt <= dateTimeProvider.UtcNow)
            return Result.Failure<string>(ShortUrlErrors.Expired);

        if (row.ClickLimit > 0 && row.TotalClicks >= row.ClickLimit)
            return Result.Failure<string>(ShortUrlErrors.ClickLimitReached);

        if (row.IsPasswordProtected)
        {
            if (request.Password is null)
                return Result.Failure<string>(ShortUrlErrors.PasswordRequired);

            if (row.PasswordHash is null || !passwordHasher.Verify(request.Password, row.PasswordHash))
                return Result.Failure<string>(ShortUrlErrors.InvalidPassword);
        }

        const string incrementSql = """
            UPDATE short_urls SET total_clicks = total_clicks + 1, updated_at_utc = @Now
            WHERE id = @Id
            """;
        await connection.ExecuteAsync(incrementSql, new { row.Id, Now = dateTimeProvider.UtcNow });

        return row.OriginalUrl;
    }

    private sealed class RedirectRow
    {
        public long Id { get; init; }
        public string OriginalUrl { get; init; } = "";
        public bool IsActive { get; init; }
        public bool IsPasswordProtected { get; init; }
        public string? PasswordHash { get; init; }
        public DateTime? ExpiresAt { get; init; }
        public int ClickLimit { get; init; }
        public int TotalClicks { get; init; }
    }
}