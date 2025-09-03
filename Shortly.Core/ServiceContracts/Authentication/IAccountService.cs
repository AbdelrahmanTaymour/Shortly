using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.DTOs.AuthDTOs.Email;

namespace Shortly.Core.ServiceContracts.Authentication;

public interface IAccountService
{
    Task<bool> SendVerificationEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AuthenticationResponse> VerifyEmailAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> InitiateEmailChangeAsync(Guid userId, string currentEmail, string newEmail, string currentPassword, CancellationToken cancellationToken = default);
    Task<bool> ConfirmEmailChangeAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ValidateResetToken(string token, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}