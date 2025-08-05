namespace Shortly.Core.DTOs.AuthDTOs.Email;

public record EmailVerificationRequest(Guid UserId, string Token);