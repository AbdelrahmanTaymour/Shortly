namespace Shortly.Core.DTOs.AuthDTOs.Email;

public record SendEmailVerificationResponse(bool EmailSent, string Message);