namespace Shortly.Core.Auth.DTOs.Email;

public record SendEmailVerificationResponse(bool EmailSent, string Message);