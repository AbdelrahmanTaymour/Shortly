using Shortly.Domain.Abstractions;
using Shortly.Domain.Enums;

namespace Shortly.Domain.ShortUrls;

public sealed class ShortUrl : Entity<long>
{
    private ShortUrl(
        OriginalUrl originalUrl,
        ShortCode shortCode,
        enShortUrlOwnerType ownerType,
        Guid? userId,
        Guid? organizationId,
        Guid? createdByMemberId,
        string? anonymousSessionId,
        string? ipAddress,
        int clickLimit,
        bool trackingEnabled,
        bool isPrivate,
        DateTime? expiresAt,
        string? title,
        string? description,
        DateTime createdAtUtc)
    {
        OriginalUrl = originalUrl;
        ShortCode = shortCode;
        OwnerType = ownerType;
        UserId = userId;
        OrganizationId = organizationId;
        CreatedByMemberId = createdByMemberId;
        AnonymousSessionId = anonymousSessionId;
        IpAddress = ipAddress;
        IsActive = true;
        TrackingEnabled = trackingEnabled;
        ClickLimit = clickLimit;
        TotalClicks = 0;
        IsPrivate = isPrivate;
        ExpiresAt = expiresAt;
        Title = title;
        Description = description;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    private ShortUrl()
    {
    }

    public OriginalUrl OriginalUrl { get; private set; }
    public ShortCode ShortCode { get; private set; }

    public enShortUrlOwnerType OwnerType { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public Guid? CreatedByMemberId { get; private set; }
    public string? AnonymousSessionId { get; private set; }
    public string? IpAddress { get; private set; }

    public bool IsActive { get; private set; }
    public bool TrackingEnabled { get; private set; }
    public int ClickLimit { get; private set; }
    public int TotalClicks { get; private set; }
    public bool IsPasswordProtected { get; private set; }
    public string? PasswordHash { get; private set; }
    public bool IsPrivate { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public string? Title { get; private set; }
    public string? Description { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Result<ShortUrl> CreateForUser(
        Guid userId,
        OriginalUrl originalUrl,
        ShortCode shortCode,
        DateTime utcNow,
        int clickLimit = -1,
        bool trackingEnabled = true,
        bool isPrivate = false,
        DateTime? expiresAt = null,
        string? title = null,
        string? description = null)
    {
        var expirationCheck = ValidateExpiration(expiresAt, utcNow);
        if (expirationCheck.IsFailure) return Result.Failure<ShortUrl>(expirationCheck.Error);

        var clickLimitCheck = ValidateClickLimit(clickLimit);
        if (clickLimitCheck.IsFailure) return Result.Failure<ShortUrl>(clickLimitCheck.Error);

        return new ShortUrl(
            originalUrl, shortCode, enShortUrlOwnerType.User,
            userId: userId, organizationId: null, createdByMemberId: null,
            anonymousSessionId: null, ipAddress: null,
            clickLimit, trackingEnabled, isPrivate, expiresAt, title, description, utcNow);
    }

    public static Result<ShortUrl> CreateForOrganization(
        Guid organizationId,
        Guid createdByMemberId,
        OriginalUrl originalUrl,
        ShortCode shortCode,
        DateTime utcNow,
        int clickLimit = -1,
        bool trackingEnabled = true,
        bool isPrivate = false,
        DateTime? expiresAt = null,
        string? title = null,
        string? description = null)
    {
        var expirationCheck = ValidateExpiration(expiresAt, utcNow);
        if (expirationCheck.IsFailure) return Result.Failure<ShortUrl>(expirationCheck.Error);

        var clickLimitCheck = ValidateClickLimit(clickLimit);
        if (clickLimitCheck.IsFailure) return Result.Failure<ShortUrl>(clickLimitCheck.Error);

        return new ShortUrl(
            originalUrl, shortCode, enShortUrlOwnerType.Organization,
            userId: null, organizationId: organizationId, createdByMemberId: createdByMemberId,
            anonymousSessionId: null, ipAddress: null,
            clickLimit, trackingEnabled, isPrivate, expiresAt, title, description, utcNow);
    }

    public static Result<ShortUrl> CreateForAnonymous(
        string anonymousSessionId,
        string ipAddress,
        OriginalUrl originalUrl,
        ShortCode shortCode,
        DateTime utcNow)
    {
        return new ShortUrl(
            originalUrl, shortCode, enShortUrlOwnerType.Anonymous,
            userId: null, organizationId: null, createdByMemberId: null,
            anonymousSessionId: anonymousSessionId, ipAddress: ipAddress,
            clickLimit: -1, trackingEnabled: false, isPrivate: false, expiresAt: null,
            title: null, description: null, utcNow);
    }

    public Result Update(
        OriginalUrl newUrl, 
        ShortCode newCode, 
        string? title, 
        string? description, 
        int clickLimit,
        bool isActive,
        bool isPrivate,
        bool isPasswordProtected,
        string? passwordHash,
        bool trackingEnabled,
        DateTime? expiresAt,
        DateTime utcNow)
    {
        var clickLimitCheck = ValidateClickLimit(clickLimit);
        if (clickLimitCheck.IsFailure) 
            return Result.Failure(ShortUrlErrors.InvalidClickLimit);
        
        var expirationCheck = ValidateExpiration(expiresAt, utcNow);
        if (expirationCheck.IsFailure) 
            return Result.Failure(ShortUrlErrors.InvalidExpiration);
        
        if(isPasswordProtected && string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure(ShortUrlErrors.PasswordRequired);
        
        OriginalUrl = newUrl;
        ShortCode = newCode;
        Title = title;
        Description = description;
        ClickLimit = clickLimit;
        IsActive = isActive;
        IsPrivate = isPrivate;
        IsPasswordProtected = isPasswordProtected;
        PasswordHash = passwordHash;
        TrackingEnabled = trackingEnabled;
        
        UpdatedAtUtc = utcNow;
        
        return Result.Success();
    }
    
    public Result AssignShortCode(ShortCode newCode, DateTime utcNow)
    {
        ShortCode = newCode;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive) return Result.Failure(ShortUrlErrors.AlreadyActive);

        IsActive = true;
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive) return Result.Failure(ShortUrlErrors.AlreadyInactive);

        IsActive = false;
        return Result.Success();
    }

    public Result SetExpiration(DateTime? expiresAt, DateTime utcNow)
    {
        var check = ValidateExpiration(expiresAt, utcNow);
        if (check.IsFailure) return check;

        ExpiresAt = expiresAt;
        return Result.Success();
    }

    public Result ProtectWithPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure(ShortUrlErrors.PasswordRequired);

        IsPasswordProtected = true;
        PasswordHash = passwordHash;
        return Result.Success();
    }

    public Result RemovePassword()
    {
        if (!IsPasswordProtected) return Result.Failure(ShortUrlErrors.NotPasswordProtected);

        IsPasswordProtected = false;
        PasswordHash = null;
        return Result.Success();
    }

    public Result RecordClick(DateTime utcNow)
    {
        var accessCheck = EnsureAccessible(utcNow);
        if (accessCheck.IsFailure) return accessCheck;

        TotalClicks++;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public bool IsExpired(DateTime utcNow) => ExpiresAt is not null && ExpiresAt <= utcNow;

    public bool HasReachedClickLimit() => ClickLimit >= 0 && TotalClicks >= ClickLimit;

    public Result EnsureAccessible(DateTime utcNow)
    {
        if (!IsActive) return Result.Failure(ShortUrlErrors.Inactive);
        if (IsExpired(utcNow)) return Result.Failure(ShortUrlErrors.Expired);
        if (HasReachedClickLimit()) return Result.Failure(ShortUrlErrors.ClickLimitReached);

        return Result.Success();
    }

    private static Result ValidateClickLimit(int clickLimit) =>
        clickLimit == -1 || clickLimit > 0
            ? Result.Success()
            : Result.Failure(ShortUrlErrors.InvalidClickLimit);

    private static Result ValidateExpiration(DateTime? expiresAt, DateTime utcNow) =>
        expiresAt is null || expiresAt > utcNow
            ? Result.Success()
            : Result.Failure(ShortUrlErrors.InvalidExpiration);
}