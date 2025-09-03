namespace Shortly.Core.DTOs.AuthDTOs.Password;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);