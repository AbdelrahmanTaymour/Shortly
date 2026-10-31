namespace Shortly.API.Controllers.Account;

public sealed record ChangeEmailRequest(string NewEmail, string Password);
