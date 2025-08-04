namespace Shortly.Core.DTOs.UsersDTOs.Profile;

public record UserProfileDto(
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