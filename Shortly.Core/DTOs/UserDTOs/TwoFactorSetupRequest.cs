using System.ComponentModel.DataAnnotations;

namespace Shortly.Core.DTOs.UserDTOs;

public class TwoFactorSetupRequest
{
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string VerificationCode { get; set; }
}

public class TwoFactorSetupResponse
{
    public string QrCodeUri { get; set; }
    public string ManualEntryKey { get; set; }
    public string[] BackupCodes { get; set; }
}

public class VerifyTwoFactorRequest
{
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; }
}

public class DisableTwoFactorRequest
{
    [Required]
    public string Password { get; set; }
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string TwoFactorCode { get; set; }
}