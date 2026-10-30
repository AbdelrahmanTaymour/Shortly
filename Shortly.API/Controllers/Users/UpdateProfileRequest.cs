namespace Shortly.API.Controllers.Users;

public sealed record UpdateProfileRequest(string? Name, string? Bio, string? PhoneNumber, string? Website, string? Company, string? Location, string? Country, string? TimeZone);
