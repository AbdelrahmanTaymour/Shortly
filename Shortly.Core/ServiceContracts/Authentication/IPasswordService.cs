using Microsoft.AspNetCore.Identity.Data;
using Shortly.Core.DTOs.AuthDTOs;

namespace Shortly.Core.ServiceContracts.Authentication;

public interface IPasswordService
{
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    Task<bool> ValidatePasswordStrengthAsync(string password);
    Task<bool> IsPasswordCompromisedAsync(string password); // Check against known breaches
}