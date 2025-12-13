using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Core.ServiceContracts.Tokens;
using Shortly.Domain.Entities;

namespace Shortly.Core.Services.Authentication;

public class OAuthService(
    IUserRepository userRepository,
    ITokenService tokenService,
    ILogger<OAuthService> logger) : IOAuthService
{
    /// <inheritdoc />
    public async Task<AuthenticationResponse?> AuthenticateWithGoogleAsync(
        string googleEmail,
        string googleId,
        string? googleName,
        string? googlePicture,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(googleEmail))
            throw new ArgumentException("Google email cannot be null or empty", nameof(googleEmail));

        if (string.IsNullOrWhiteSpace(googleId))
            throw new ArgumentException("Google ID cannot be null or empty", nameof(googleId));

        try
        {
            // Check if the user exists by GoogleID first (the most specific lookup)
            var existingUser = await userRepository.GetUserByGoogleIdAsync(googleId, cancellationToken);
            if (existingUser != null) 
                return await HandleExistingGoogleUserAsync(existingUser, googlePicture);

            // Check if the user exists by email (for account linking)
            existingUser = await userRepository.GetByEmailAsync(googleEmail, cancellationToken);
            if (existingUser != null)
                return await LinkGoogleAccountAsync(existingUser, googleId, googlePicture, googleEmail);

            // Create a new user
            return await CreateNewGoogleUserAsync(googleEmail, googleId, googleName, googlePicture);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error during Google OAuth authentication for email {Email}. GoogleId: {GoogleId}",
                googleEmail,
                googleId);
            return null;
        }
    }


    /// <summary>
    ///     Handles authentication for existing Google users
    /// </summary>
    private async Task<AuthenticationResponse> HandleExistingGoogleUserAsync(
        User user,
        string? googlePicture)
    {
        var now = DateTime.UtcNow;

        // Update the profile picture if it has changed
        if (!string.IsNullOrWhiteSpace(googlePicture) && user.GoogleProfilePicture != googlePicture)
            user.GoogleProfilePicture = googlePicture;

        user.LastLoginAt = now;
        user.UpdatedAt = now;
        await userRepository.UpdateAsync(user);

        logger.LogInformation("User {UserId} logged in via Google OAuth", user.Id);
        return await GenerateAuthenticationResponseAsync(user);
    }

    /// <summary>
    ///     Links a Google account to an existing user
    /// </summary>
    private async Task<AuthenticationResponse> LinkGoogleAccountAsync(
        User user,
        string googleId,
        string? googlePicture,
        string googleEmail)
    {
        var now = DateTime.UtcNow;

        user.GoogleId = googleId;
        user.GoogleProfilePicture = googlePicture;
        user.IsOAuthUser = true;
        user.IsEmailConfirmed = true;
        user.LastLoginAt = now;
        user.UpdatedAt = now;

        await userRepository.UpdateAsync(user);

        logger.LogInformation(
            "Linked Google an account to existing user {UserId} with email {Email}",
            user.Id,
            googleEmail);

        return await GenerateAuthenticationResponseAsync(user);
    }

    /// <summary>
    ///     Creates a new user from Google OAuth
    /// </summary>
    private async Task<AuthenticationResponse> CreateNewGoogleUserAsync(
        string googleEmail,
        string googleId,
        string? googleName,
        string? googlePicture)
    {
        var now = DateTime.UtcNow;
        var username = _GenerateUsernameFromEmail(googleEmail, googleName);

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = googleEmail,
            Username = username,
            PasswordHash = _GenerateRandomPasswordHash(),
            GoogleId = googleId,
            GoogleProfilePicture = googlePicture,
            IsOAuthUser = true,
            IsEmailConfirmed = true,
            IsActive = true,
            LastLoginAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await userRepository.CreateAsync(newUser);

        logger.LogInformation(
            "Created new user {UserId} via Google OAuth with email {Email}",
            newUser.Id,
            googleEmail);

        return await GenerateAuthenticationResponseAsync(newUser);
    }

    /// <summary>
    ///     Generates authentication response with JWT tokens
    /// </summary>
    private async Task<AuthenticationResponse> GenerateAuthenticationResponseAsync(User user)
    {
        var accessToken = await tokenService.GenerateTokensAsync(user.MapToUserDto());

        return new AuthenticationResponse(
            user.Id,
            user.Email,
            accessToken,
            true,
            user.IsEmailConfirmed
        );
    }

    /// <summary>
    ///     Generates a username from email and name
    /// </summary>
    private string _GenerateUsernameFromEmail(string email, string? name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            // Use name and add random suffix
            var cleanName = new string(name.Where(char.IsLetterOrDigit).ToArray());
            return $"{cleanName}_{Guid.NewGuid().ToString()[..8]}".ToLower();
        }

        // Fallback to email prefix
        var emailPrefix = email.Split('@')[0];
        var cleanPrefix = new string(emailPrefix.Where(char.IsLetterOrDigit).ToArray());
        return $"{cleanPrefix}_{Guid.NewGuid().ToString()[..8]}".ToLower();
    }

    /// <summary>
    ///     Generates a random password hash for OAuth users (they won't use it)
    /// </summary>
    private string _GenerateRandomPasswordHash()
    {
        return BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());
    }
}