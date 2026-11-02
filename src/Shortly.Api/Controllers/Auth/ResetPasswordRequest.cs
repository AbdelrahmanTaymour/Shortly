namespace Shortly.Api.Controllers.Auth;

public sealed record ResetPasswordRequest(string Token, string NewPassword);