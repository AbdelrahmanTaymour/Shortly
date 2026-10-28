namespace Shortly.API.Controllers.Auth;

public sealed record InitiateEmailChangeRequest(string NewEmail, string CurrentPassword);