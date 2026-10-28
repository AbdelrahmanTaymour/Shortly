namespace Shortly.API.Controllers.Account;

public sealed record ResetPasswordRequest(string Token, string NewPassword);
