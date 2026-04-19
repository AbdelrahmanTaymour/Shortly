namespace Shortly.Core.Auth.DTOs.Password;

public record ResetPasswordRequest(string Token, string NewPassword, string ConfirmPassword);