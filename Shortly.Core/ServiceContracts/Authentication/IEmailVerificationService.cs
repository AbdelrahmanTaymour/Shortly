using Shortly.Core.DTOs.AuthDTOs.Email;

namespace Shortly.Core.ServiceContracts.Authentication;

/// <summary>
/// Email verification and confirmation
/// </summary>
public interface IEmailVerificationService
{
    Task<bool> SendEmailVerificationAsync(Guid userId);
    Task<bool> VerifyEmailAsync(EmailVerificationRequest request, CancellationToken cancellationToken = default);
    Task<bool> ResendEmailVerificationAsync(ResendEmailVerificationRequest request);
    Task<bool> IsEmailVerifiedAsync(Guid userId);
    Task<bool> GenerateEmailVerificationTokenAsync(Guid userId);
}