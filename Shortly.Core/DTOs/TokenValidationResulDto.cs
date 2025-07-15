using System.Security.Claims;

namespace Shortly.Core.DTOs;

public record TokenValidationResulDto(bool IsValid, ClaimsPrincipal? Principal = null, string? ErrorMessage = null);