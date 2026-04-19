namespace Shortly.Core.Profile.DTOs;

public record UserProfileResponse(
    string? Name,
    string? Bio,
    string? PhoneNumber,
    string? ProfilePictureUrl,
    string? Website,
    string? Company,
    string? Location,
    string? Country,
    string? TimeZone,
    DateTime UpdatedAt
);