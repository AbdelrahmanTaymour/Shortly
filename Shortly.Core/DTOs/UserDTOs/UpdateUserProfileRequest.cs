using System.ComponentModel.DataAnnotations;

namespace Shortly.Core.DTOs.UserDTOs;

public class UpdateUserProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }
    
    [StringLength(50)]
    public string? TimeZone { get; set; }
    
    [Url]
    public string? ProfilePictureUrl { get; set; }
}