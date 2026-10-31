namespace Shortly.API.Controllers.Account;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
