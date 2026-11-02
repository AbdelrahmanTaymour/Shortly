namespace Shortly.Api.Controllers.Account;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
