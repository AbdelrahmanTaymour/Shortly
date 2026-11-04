using System.Threading.RateLimiting;
using Shortly.Infrastructure.EdgeProtection.Models;

namespace Shortly.Infrastructure.EdgeProtection.Services;

/// <summary>
/// Maintains an in-memory, per-instance budget of failed password attempts against individual short links.
/// </summary>
/// <remarks>
/// SECURITY PURPOSE: 
/// Mitigates distributed botnets targeting a single password-protected link across multiple IP addresses.
/// - The key is always a short code extracted from a cryptographically signed token, preventing memory-exhaustion attacks.
/// - Idle rate-limiting partitions are automatically cleaned up by ASP.NET Core's underlying rate limiter.
/// </remarks>
public sealed class LinkPasswordAttemptLimiter(PasswordVerificationPerLinkRateLimitOptions options) : IDisposable
{
    private readonly PartitionedRateLimiter<string> _limiter = PartitionedRateLimiter.Create<string, string>(shortCode =>
        RateLimitPartition.GetFixedWindowLimiter(shortCode, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.PermitLimit,
            Window = TimeSpan.FromSeconds(options.WindowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    /// <summary>
    /// Checks whether a specific short link has exhausted its failed-attempt budget.
    /// </summary>
    /// <param name="shortCode">The unique identifier of the protected link.</param>
    /// <param name="retryAfter">Outputs the remaining cooldown duration if the link is locked; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the link is locked due to too many failed attempts; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This only checks if the link is currently locked. 
    /// It does NOT count as a new failed attempt, so it is safe to call without changing the failure count.
    /// </remarks>
    public bool IsLocked(string shortCode, out TimeSpan? retryAfter)
    {
        using var lease = _limiter.AttemptAcquire(shortCode, 0);
        if (lease.IsAcquired)
        {
            retryAfter = null;
            return false;
        }

        retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var wait) ? wait : null;
        return true;
    }

    /// <summary>Counts one wrong password against the link.</summary>
    public void RecordFailure(string shortCode) => _limiter.AttemptAcquire(shortCode).Dispose();

    public void Dispose() => _limiter.Dispose();
}
