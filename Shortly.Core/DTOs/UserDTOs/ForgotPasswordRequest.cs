using System.ComponentModel.DataAnnotations;

namespace Shortly.Core.DTOs.UserDTOs;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}