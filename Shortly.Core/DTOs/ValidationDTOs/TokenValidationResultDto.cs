using System.Security.Claims;

namespace Shortly.Core.DTOs.ValidationDTOs;

public record TokenValidationResultDto(bool IsValid, ClaimsPrincipal? Principal = null, string? ErrorMessage = null);