namespace Shortly.API.Controllers.Auth;

public sealed record ResetPasswordRequest(string Token, string NewPassword);