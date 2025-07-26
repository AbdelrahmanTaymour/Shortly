using System.ComponentModel.DataAnnotations;

namespace Shortly.Core.DTOs.UserDTOs;

public class EmailVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    public string VerificationToken { get; set; }
}

public class ResendEmailVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}