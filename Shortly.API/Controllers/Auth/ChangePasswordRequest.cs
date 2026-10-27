namespace Shortly.API.Controllers.Auth;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
