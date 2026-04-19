using System.Security.Claims;

namespace Shortly.Core.Auth.DTOs;

public record TokenValidationResultDto(bool IsValid, ClaimsPrincipal? Principal = null, string? ErrorMessage = null);