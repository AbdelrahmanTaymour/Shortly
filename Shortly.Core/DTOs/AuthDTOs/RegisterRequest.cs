namespace Shortly.Core.DTOs.AuthDTOs;

public record RegisterRequest(string Name, string Email, string Username, string Password)
{
    // Parameterless constructor
    public RegisterRequest(): this(String.Empty, String.Empty, String.Empty, String.Empty) { }
}