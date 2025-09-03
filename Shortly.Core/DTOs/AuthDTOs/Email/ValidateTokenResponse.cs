namespace Shortly.Core.DTOs.AuthDTOs.Email;

public record ValidateTokenResponse(bool IsValid, string? Message);