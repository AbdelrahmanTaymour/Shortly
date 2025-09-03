using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.DTOs.UsersDTOs.User;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;

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
    /// The <see cref="UserDto"/> object if credentials are valid.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when no user is found with the specified email or username.</exception>
    /// <exception cref="UnauthorizedException">Thrown when the password does not match the stored hash for the user.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<UserDto> ValidateCredentialsAsync(string emailOrUsername, string password,
        CancellationToken cancellationToken = default);
}