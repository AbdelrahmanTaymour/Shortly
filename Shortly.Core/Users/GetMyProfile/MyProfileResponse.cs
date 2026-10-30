namespace Shortly.Core.Users.GetMyProfile;

public sealed record MyProfileResponse(
    Guid Id, string Email, string Username, bool IsEmailConfirmed, DateTime CreatedAtUtc,
    string? Name, string? Bio, string? PhoneNumber, string? ProfilePictureUrl,
    string? Website, string? Company, string? Location, string? Country, string? TimeZone);