namespace Shortly.Core.Auth.DTOs.Email;

public record ValidateTokenResponse(bool IsValid, string? Message);