using Shortly.Core.DTOs.AuthDTOs.TwoFactor;

namespace Shortly.Core.ServiceContracts.Authentication;

/// <summary>
/// Two-factor authentication management
/// </summary>
public interface ITwoFactorAuthenticationService
{
    Task<TwoFactorSetupResponse?> SetupTwoFactorAsync(Guid userId);
    Task<bool> EnableTwoFactorAsync(Guid userId, TwoFactorSetupRequest request);
    Task<bool> DisableTwoFactorAsync(Guid userId, DisableTwoFactorRequest request);
    Task<bool> VerifyTwoFactorCodeAsync(Guid userId, string code);
    Task<string[]> GenerateBackupCodesAsync(Guid userId);
    Task<bool> ValidateBackupCodeAsync(Guid userId, string backupCode);
    Task<bool> IsTwoFactorEnabledAsync(Guid userId);
}