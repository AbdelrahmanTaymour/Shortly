namespace Shortly.Core.DTOs.AuthDTOs.Password;

public record ResetPasswordRequest(string Token, string NewPassword, string ConfirmPassword);