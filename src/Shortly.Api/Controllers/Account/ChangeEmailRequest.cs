namespace Shortly.Api.Controllers.Account;

public sealed record ChangeEmailRequest(string NewEmail, string Password);
