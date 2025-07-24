using System.Security.Claims;

namespace Shortly.Core.DTOs.AuthDTOs;

public record TokenValidationResulDto(bool IsValid, ClaimsPrincipal? Principal = null, string? ErrorMessage = null);