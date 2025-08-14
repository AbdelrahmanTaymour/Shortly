using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

/// <summary>
/// Provides extension methods for mapping between ShortUrl domain entities and their corresponding DTOs.
/// </summary>
public static class ShortUrlMapper
{
    public static ShortUrlDto MapToShortUrlDto(this ShortUrl url)
    {
        return new ShortUrlDto
        {
            Id = url.Id,
            OriginalUrl = url.OriginalUrl,
            ShortCode = url.ShortCode  ?? string.Empty,
            OwnerType = url.OwnerType,
            UserId = url.UserId,
            OrganizationId = url.OrganizationId,
            CreatedByMemberId = url.CreatedByMemberId,
            IsActive = url.IsActive,
            TrackingEnabled = url.TrackingEnabled,
            ClickLimit = url.ClickLimit,
            TotalClicks = url.TotalClicks,
            IsPasswordProtected = url.IsPasswordProtected,
            IsPrivate = url.IsPrivate,
            ExpiresAt = url.ExpiresAt,
            Title = url.Title,
            Description = url.Description,
            CreatedAt = url.CreatedAt,
            UpdatedAt = url.UpdatedAt,
        };
    }

    public static IEnumerable<ShortUrlDto> MapToShortUrlDtos(this IEnumerable<ShortUrl> urls)
    {
        return urls.Select(MapToShortUrlDto);
    }

    public static CreateShortUrlResponse MapToCreateShortUrlResponse(this ShortUrl url)
    {
        return new CreateShortUrlResponse
        (
            url.Id,
            url.OriginalUrl,
            url.ShortCode ?? throw new InvalidOperationException(),
            url.CreatedAt
        );
    }
}