namespace Shortly.Core.Models;

public record TokenSettings
{
    public int EmailVerificationExpiryHours { get; set; } = 24;
    public int PasswordResetExpiryHours { get; set; } = 1;
    public int InvitationExpiryDays { get; set; } = 7;
}