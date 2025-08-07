namespace Shortly.Core.DTOs.AuthDTOs;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);