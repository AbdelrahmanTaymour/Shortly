namespace Shortly.Core.DTOs;

public record LoginRequest(string Email, string Password)
{
    // Parameterless constructor
    public LoginRequest(): this(String.Empty, String.Empty) { }
};