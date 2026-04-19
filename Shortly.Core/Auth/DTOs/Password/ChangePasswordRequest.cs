namespace Shortly.Core.Auth.DTOs.Password;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);