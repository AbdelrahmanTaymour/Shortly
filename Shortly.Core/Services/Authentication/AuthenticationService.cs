using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Domain.Entities;

namespace Shortly.Core.Services.Authentication;

/// <summary>
/// Authenticates and registers users, handling password validation and token issuance.
/// </summary>
public class AuthenticationService(IUserRepository userRepository,IRefreshTokenRepository refreshTokenRepository,
    ILogger<AuthenticationService> logger, ITokenService tokenService): IAuthenticationService
{
    private readonly IUserRepository _userRepository = userRepository;

    /// <inheritdoc />
    /// <exception cref="NotFoundException">Thrown when no user matches the given email or username.</exception>
    /// <exception cref="UnauthorizedException">Thrown when the password does not match.</exception>
    public async Task<AuthenticationResponse?> Login(LoginRequest loginRequest, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailOrUsernameAsync(loginRequest.EmailOrUsername, cancellationToken);
        if (user == null)
            throw new NotFoundException("User with the specified email was not found.");

        //Verify the password matches the hashed password
        if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        // Generate access and refresh tokens
        var tokensResponse = await tokenService.GenerateTokensAsync(user);
        
        logger.LogInformation("User with id {userId} logged in successfully.", user.Id);;
        return new AuthenticationResponse(user.Id, user.Email, tokensResponse, true, !user.IsEmailConfirmed);
    }
    
    /// <inheritdoc />
    /// <exception cref="ConflictException">Thrown when the email or username already exists.</exception>
    /// <exception cref="DatabaseException">Thrown when user creation fails.</exception>
    public async Task<AuthenticationResponse?> Register(RegisterRequest registerRequest, CancellationToken cancellationToken = default)
    {
        bool userExists =
            await _userRepository.EmailOrUsernameExistsAsync(registerRequest.Email, registerRequest.Username,
                cancellationToken);
        
        if (userExists)
        {
            throw new ConflictException("user", "email or username");
        }
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = registerRequest.Email,
            Username = registerRequest.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password,10), // Hash the PasswordHash
        };
        
        user = await _userRepository.CreateAsync(user, cancellationToken);
        if(user == null)
        {
            throw new DatabaseException("Failed to create user. Please try again later.");
        }
        
        var tokensResponse = await tokenService.GenerateTokensAsync(user);
        return new AuthenticationResponse(user.Id, user.Email, tokensResponse, true, !user.IsEmailConfirmed);
    }
}