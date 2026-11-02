namespace Shortly.Api.Controllers.Auth;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
