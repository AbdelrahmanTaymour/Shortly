using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Shortly.API.Controllers.Base;

/// <summary>
///     Base controller class providing common functionality for API controllers.
///     Includes user authentication, authorization, and claim extraction utilities.
/// </summary>
/// <remarks>
///     This base class should be inherited by all API controllers that require user authentication.
///     It provides secure methods to extract user information from JWT claims with proper validation
///     and error handling.
/// </remarks>
public abstract class ControllerApiBase : ControllerBase
{
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    protected ControllerApiBase() { }
    private readonly ILogger<ControllerApiBase> _logger;

    /// <summary>
    ///     Cached current user ID to avoid repeated claim parsing within the same request.
    /// </summary>
    private Guid? _cachedUserId;

    /// <summary>
    ///     Cached current user email to avoid repeated claim parsing within the same request.
    /// </summary>
    private string? _cachedUserEmail;

    /// <summary>
    ///     Cached current user permissions to avoid repeated claim parsing within the same request.
    /// </summary>
    private long? _cachedUserPermissions;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ControllerApiBase" /> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging operations and errors.</param>
    protected ControllerApiBase(ILogger<ControllerApiBase> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Retrieves the current authenticated user's unique identifier from JWT claims.
    /// </summary>
    /// <returns>
    ///     The current user's GUID identifier.
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">
    ///     Thrown when the user is not authenticated or the NameIdentifier claim is missing.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the user ID claim value is not in a valid GUID format.
    /// </exception>
    /// <example>
    ///     <code>
    /// try
    /// {
    ///     var userId = GetCurrentUserId();
    ///     // Use userId for business logic
    /// }
    /// catch (UnauthorizedAccessException ex)
    /// {
    ///     // Handle authentication error
    ///     return Unauthorized (ex.Message);
    /// }
    /// </code>
    /// </example>
    protected Guid GetCurrentUserId()
    {
        // Return cached value if available
        if (_cachedUserId.HasValue) return _cachedUserId.Value;

        try
        {
            var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(nameIdentifierClaim))
            {
                _logger.LogWarning("NameIdentifier claim is missing or empty for user: {UserIdentity}",
                    User.Identity?.Name ?? "Unknown");
                throw new UnauthorizedAccessException(
                    "User authentication is required. Unable to determine current user ID.");
            }

            if (!Guid.TryParse(nameIdentifierClaim, out var userId))
            {
                _logger.LogError("Invalid user ID format in NameIdentifier claim: {ClaimValue}", nameIdentifierClaim);
                throw new InvalidOperationException($"Invalid user ID format: {nameIdentifierClaim}");
            }

            // Cache the result
            _cachedUserId = userId;

            _logger.LogDebug("Successfully retrieved user ID: {UserId}", userId);
            return userId;
        }
        catch (Exception ex) when (!(ex is UnauthorizedAccessException || ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving current user ID");
            throw new InvalidOperationException("An error occurred while processing user authentication", ex);
        }
    }

    /// <summary>
    ///     Retrieves the current authenticated user's email address from JWT claims.
    /// </summary>
    /// <returns>
    ///     The current user's email address, or an empty string if the email claim is not present.
    /// </returns>
    /// <remarks>
    ///     This method does not throw exceptions for missing email claims, as email might be optional
    ///     depending on the authentication provider. Returns an empty string if no email is found.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var userEmail = GetCurrentEmail();
    /// if (!string.IsNullOrEmpty(userEmail))
    /// {
    ///     // Send notification to user email
    /// }
    /// </code>
    /// </example>
    protected string GetCurrentEmail()
    {
        // Return cached value if available
        if (_cachedUserEmail != null) return _cachedUserEmail;

        try
        {
            var userEmailClaim = User.FindFirst(ClaimTypes.Email)?.Value;

            // Cache the result (even if it's null/empty)
            _cachedUserEmail = userEmailClaim ?? string.Empty;

            _logger.LogDebug(
                string.IsNullOrWhiteSpace(_cachedUserEmail)
                    ? "Email claim is missing or empty for user: {UserId}"
                    : "Successfully retrieved user email for user: {UserId}",
                GetCurrentUserId().ToString());

            return _cachedUserEmail;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error occurred while retrieving current user email, returning empty string");
            return string.Empty;
        }
    }

    /// <summary>
    ///     Retrieves the current authenticated user's permission flags from JWT claims.
    /// </summary>
    /// <returns>
    ///     A long integer representing the user's permission flags as a bitmask,
    ///     or 0 if no permissions claim is present or if the claim value is invalid.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Permissions are typically stored as a bitmask where each bit represents a specific permission.
    ///         This allows for efficient storage and checking of multiple permissions.
    ///     </para>
    ///     <para>
    ///         If the permissions claim is missing or cannot be parsed as a valid long integer,
    ///         this method returns 0, indicating no permissions.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var permissions = GetCurrentUserPermissions();
    /// const long ReadPermission = 1;
    /// const long WritePermission = 2;
    /// 
    /// if ((permissions &amp; ReadPermission) != 0)
    /// {
    ///     // User has read permission
    /// }
    /// 
    /// if ((permissions &amp; WritePermission) != 0)
    /// {
    ///     // User has write permission
    /// }
    /// </code>
    /// </example>
    private long GetCurrentUserPermissions()
    {
        // Return cached value if available
        if (_cachedUserPermissions.HasValue) return _cachedUserPermissions.Value;

        try
        {
            var permissionsClaim = User.FindFirst("Permissions")?.Value;

            long permissions = 0;

            if (!string.IsNullOrWhiteSpace(permissionsClaim))
            {
                if (long.TryParse(permissionsClaim, out permissions))
                    _logger.LogDebug("Successfully retrieved user permissions: {Permissions} for user: {UserId}",
                        permissions, GetCurrentUserId().ToString());
                else
                    _logger.LogWarning("Invalid permissions claim format: {PermissionsClaim} for user: {UserId}",
                        permissionsClaim, GetCurrentUserId().ToString());
            }
            else
            {
                _logger.LogDebug("Permissions claim is missing or empty for user: {UserId}",
                    GetCurrentUserId().ToString());
            }

            // Cache the result
            _cachedUserPermissions = permissions;
            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error occurred while retrieving current user permissions, returning 0");
            return 0;
        }
    }

    /// <summary>
    ///     Checks if the current user has the specified permission.
    /// </summary>
    /// <param name="permission">The permission flag to check (as a power of 2).</param>
    /// <returns>
    ///     <c>true</c> if the user has the specified permission; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This method is useful for conditional logic within controller actions.
    ///     For endpoint-level authorization, consider using authorization policies instead.
    ///     Uses the same bitwise logic as your authorization handler.
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Example: Return different data based on user permissions
    /// public IActionResult GetUsers()
    /// {
    ///     var users = userService.GetUsers();
    ///     
    ///     if (!HasPermission(UserPermissions.ViewSensitiveData))
    ///     {
    ///         // Remove sensitive fields for users without permission
    ///         users = users.Select(u => new { u.Id, u.Name }).ToList();
    ///     }
    ///     
    ///     return Ok(users);
    /// }
    /// </code>
    /// </example>
    protected bool HasPermission(long permission)
    {
        var userPermissions = GetCurrentUserPermissions();
        return (userPermissions & permission) == permission; // Fixed: should be == not !=
    }

    /// <summary>
    ///     Checks if the current user has all the specified permissions.
    /// </summary>
    /// <param name="permissions">The permission flags to check (can be combined with bitwise OR).</param>
    /// <returns>
    ///     <c>true</c> if the user has all specified permissions; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Uses the same bitwise logic as your authorization handler for consistency.
    /// </remarks>
    /// <example>
    ///     <code>
    /// const long ReadPermission = 1;
    /// const long WritePermission = 2;
    /// 
    /// if (HasAllPermissions(ReadPermission | WritePermission))
    /// {
    ///     // User can both read and write
    /// }
    /// </code>
    /// </example>
    protected bool HasAllPermissions(long permissions)
    {
        var userPermissions = GetCurrentUserPermissions();
        return (userPermissions & permissions) == permissions;
    }

    /// <summary>
    ///     Checks if the current user has any of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permission flags to check (can be combined with bitwise OR).</param>
    /// <returns>
    ///     <c>true</c> if the user has at least one of the specified permissions; otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    ///     <code>
    /// const long ReadPermission = 1;
    /// const long WritePermission = 2;
    /// 
    /// if (HasAnyPermission(ReadPermission | WritePermission))
    /// {
    ///     // User can either read or write (or both)
    /// }
    /// </code>
    /// </example>
    protected bool HasAnyPermission(long permissions)
    {
        var userPermissions = GetCurrentUserPermissions();
        return (userPermissions & permissions) != 0;
    }

    /// <summary>
    ///     Gets a claim value by claim type from the current user's claims.
    /// </summary>
    /// <param name="claimType">The type of claim to retrieve.</param>
    /// <returns>
    ///     The claim value if found; otherwise, <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="claimType" /> is null or whitespace.
    /// </exception>
    /// <example>
    ///     <code>
    /// var department = GetClaimValue("Department");
    /// if (!string.IsNullOrEmpty(department))
    /// {
    ///     // Use department information
    /// }
    /// </code>
    /// </example>
    protected string? GetClaimValue(string claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType))
            throw new ArgumentException("Claim type cannot be null or whitespace", nameof(claimType));

        try
        {
            var claimValue = User.FindFirst(claimType)?.Value;
            _logger.LogDebug("Retrieved claim {ClaimType}: {HasValue} for user: {UserId}",
                claimType,
                !string.IsNullOrEmpty(claimValue) ? "Present" : "Missing",
                GetCurrentUserId().ToString());

            return claimValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error occurred while retrieving claim {ClaimType}", claimType);
            return null;
        }
    }
}