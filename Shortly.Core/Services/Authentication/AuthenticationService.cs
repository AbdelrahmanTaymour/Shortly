using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.Tokens;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Domain.Entities;

namespace Shortly.Core.Services.Authentication;

/// <summary>
/// Authenticates and registers users, handling password validation and token issuance.
/// </summary>
public class AuthenticationService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ILogger<AuthenticationService> logger,
    ITokenService tokenService) : IAuthenticationService
{
    private readonly IUserRepository _userRepository = userRepository;

    /// <inheritdoc />
    public async Task<AuthenticationResponse?> Login(LoginRequest loginRequest,
        CancellationToken cancellationToken = default)
    {
        var user = await ValidateCredentialsAsync(loginRequest.EmailOrUsername, loginRequest.Password,
            cancellationToken);

        // Generate access and refresh tokens
        var tokensResponse = await tokenService.GenerateTokensAsync(user);
        logger.LogInformation("User with id {userId} logged in successfully.", user.Id);
        
        return new AuthenticationResponse(user.Id, user.Email, tokensResponse, true, !user.IsEmailConfirmed);
    }

    /// <inheritdoc />
    /// <exception cref="ConflictException">Thrown when the email or username already exists.</exception>
    /// <exception cref="DatabaseException">Thrown when user creation fails.</exception>
    public async Task<AuthenticationResponse?> Register(RegisterRequest registerRequest,
        CancellationToken cancellationToken = default)
    {
        var userExists =
            await _userRepository.EmailOrUsernameExistsAsync(registerRequest.Email, registerRequest.Username,
                cancellationToken);

        if (userExists) throw new ConflictException("user", "email or username");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = registerRequest.Email,
            Username = registerRequest.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password, 10) // Hash the PasswordHash
        };

        user = await _userRepository.CreateAsync(user);
        if (user == null) throw new DatabaseException("Failed to create user. Please try again later.");

        var tokensResponse = await tokenService.GenerateTokensAsync(user);
        
        return new AuthenticationResponse(user.Id, user.Email, tokensResponse, true, !user.IsEmailConfirmed);
    }

    /// <inheritdoc/>
    /// <exception cref="NotFoundException">
    /// Thrown when no user is found with the specified email or username.
    /// </exception>
    /// <exception cref="UnauthorizedException">
    /// Thrown when the password does not match the stored hash for the user.
    /// </exception>
    public async Task<User> ValidateCredentialsAsync(string emailOrUsername, string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailOrUsernameAsync(emailOrUsername, cancellationToken);
        if (user == null)
            throw new NotFoundException("User with the specified email was not found.");

        //Verify the password matches the hashed password
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        return user;
    }
}