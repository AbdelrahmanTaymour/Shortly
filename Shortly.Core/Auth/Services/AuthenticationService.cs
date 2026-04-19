using Microsoft.Extensions.Logging;
using Shortly.Core.Auth.Contracts;
using Shortly.Core.Auth.DTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Tokens.Contracts;
using Shortly.Core.Users.Contracts;
using Shortly.Core.Users.DTOs.User;
using Shortly.Core.Users.Mappers;

namespace Shortly.Core.Auth.Services;

/// <summary>
/// Authenticates and registers users, handling password validation and token issuance.
/// </summary>
public class AuthenticationService(
    IUserService userService,
    IAccountService accountService,
    ITokenService tokenService,
    ILogger<AuthenticationService> logger
    ) : IAuthenticationService
{
    /// <inheritdoc />
    public async Task<AuthenticationResponse?> Login(LoginRequest loginRequest,
        CancellationToken cancellationToken = default)
    {
        var user = await ValidateCredentialsAsync(loginRequest.EmailOrUsername, loginRequest.Password, cancellationToken);

        // Generate access and refresh tokens
        var tokensResponse = await tokenService.GenerateTokensAsync(user);
        logger.LogInformation("User with id {userId} logged in successfully.", user.Id);
        
        return new AuthenticationResponse(user.Id, user.Email, tokensResponse, true, !user.IsEmailConfirmed);
    }

    /// <inheritdoc />
    /// <exception cref="ConflictException">Thrown when the email or username already exists.</exception>
    /// <exception cref="DatabaseException">Thrown when user creation fails.</exception>
    public async Task<AuthenticationResponse?> Register(RegisterRequest registerRequest, CancellationToken cancellationToken = default)
    {
        var userExists =
            await userService.EmailOrUsernameExistsAsync(registerRequest.Email, registerRequest.Username,
                cancellationToken);

        if (userExists) throw new ConflictException("user", "email or username");

        var user = new CreateUserRequest
        (
            Email: registerRequest.Email,
            Username: registerRequest.Username,
            Password: registerRequest.Password
        );

        var createUser = await userService.CreateAsync(user, cancellationToken);
        
        // Send email verification and ignore any exception to not brock the registration flow 
        try
        {
            await accountService.SendVerificationEmailAsync(createUser.Email, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email verification while registering user with email {Email}.", createUser.Email);
        }
        
        var tokensResponse = await tokenService.GenerateTokensAsync(createUser);
        
        return new AuthenticationResponse(
            Id: createUser.Id,
            Email: createUser.Email,
            Tokens: tokensResponse,
            Success: true,
            RequiresEmailConfirmation: !createUser.IsEmailConfirmed);
    }

    /// <inheritdoc/>
    public async Task<UserDto> ValidateCredentialsAsync(string emailOrUsername, string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userService.GetByEmailOrUsernameAsync(emailOrUsername, cancellationToken);
        
        //Verify the password matches the hashed password
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        return user.MapToUserDto();
    }
}