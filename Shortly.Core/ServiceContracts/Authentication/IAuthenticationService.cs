using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Domain.Entities;

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

    /// <summary>
    /// Validates the provided user credentials by verifying the email or username and password.
    /// </summary>
    /// <param name="emailOrUsername">The user's email address or username.</param>
    /// <param name="password">The plaintext password to verify against the stored hash.</param>
    /// <param name="cancellationToken">Optional cancellation token for the asynchronous operation.</param>
    /// <returns>
    /// The <see cref="User"/> object if credentials are valid.
    /// </returns>
    Task<User> ValidateCredentialsAsync(string emailOrUsername, string password,
        CancellationToken cancellationToken = default);
}