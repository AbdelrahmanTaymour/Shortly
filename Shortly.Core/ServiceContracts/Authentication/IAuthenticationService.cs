using Shortly.Core.DTOs.AuthDTOs;

namespace Shortly.Core.ServiceContracts.Authentication;

/// <summary>
/// Defines authentication operations such as login and registration.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user using email/username and password.
    /// </summary>
    /// <param name="loginRequest">The login request containing credentials.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An <see cref="AuthenticationResponse"/> if successful; otherwise, null.</returns>
    Task<AuthenticationResponse?> Login(LoginRequest loginRequest, CancellationToken cancellationToken);
    
    /// <summary>
    /// Registers a new user with the provided credentials.
    /// </summary>
    /// <param name="registerRequest">The registration request containing user information.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An <see cref="AuthenticationResponse"/> if successful; otherwise, null.</returns>
    Task<AuthenticationResponse?> Register(RegisterRequest registerRequest, CancellationToken cancellationToken);

}