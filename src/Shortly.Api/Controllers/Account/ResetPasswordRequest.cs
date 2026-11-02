namespace Shortly.Api.Controllers.Account;

public sealed record ResetPasswordRequest(string Token, string NewPassword);
