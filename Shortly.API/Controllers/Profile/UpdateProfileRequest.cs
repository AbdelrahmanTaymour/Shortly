namespace Shortly.API.Controllers.Profile;

public sealed record UpdateProfileRequest(string? Name, string? Bio, string? PhoneNumber, string? Website, string? Company, string? Location, string? Country, string? TimeZone);
