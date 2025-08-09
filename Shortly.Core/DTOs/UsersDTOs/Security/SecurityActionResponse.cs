namespace Shortly.Core.DTOs.UsersDTOs.Security;

public record SecurityActionResponse
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable message describing the operation result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the user affected by the operation.
    /// </summary>
    public Guid UserId { get; set; }
}