namespace Shortly.Core.Security.DTOs;

public record SecurityActionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}