namespace Shortly.Core.DTOs.AuthDTOs;

public record LoginRequest(string Email, string Password)
{
    // Parameterless constructor
    public LoginRequest(): this(String.Empty, String.Empty) { }
};