namespace Shortly.Api.Controllers.Auth;

public sealed record InitiateEmailChangeRequest(string NewEmail, string CurrentPassword);