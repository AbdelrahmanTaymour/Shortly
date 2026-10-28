using System.Net;
using System.Net.Sockets;
using MaxMind.Db;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shortly.Core.Abstractions.ClickTracking;

namespace Shortly.Infrastructure.ClickTracking;

internal sealed class MaxMindGeoLocationService
    : IGeoLocationService, IDisposable
{
    private readonly GeoLite2Options _options;
    private readonly ILogger<MaxMindGeoLocationService> _logger;
    private readonly ReaderWriterLockSlim _lock = new();

    private DatabaseReader? _reader;
    private bool _disposed;

    public MaxMindGeoLocationService(
        IOptions<GeoLite2Options> options,
        ILogger<MaxMindGeoLocationService> logger)
    {
        _options = options.Value;
        _logger = logger;

        _reader = TryCreateReader();
    }

    public Task<GeoLocationInfo> LookupAsync(
        string ipAddress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IPAddress.TryParse(ipAddress, out var ip) || IsNonPublicAddress(ip))
        {
            return Task.FromResult(Unknown());
        }

        _lock.EnterReadLock();

        try
        {
            if (_reader is not null && _reader.TryCity(ip, out var response))
            {
                return Task.FromResult(
                    new GeoLocationInfo(
                        response.Country.Name ?? "Unknown", response.City.Name ?? "Unknown"));
            }
        }
        catch (AddressNotFoundException)
        {
            // No GeoLite2 record exists for this address.
        }
        catch (Exception ex)
        {
            // Avoid logging the visitor's IP address.
            _logger.LogWarning(ex, "GeoLite2 lookup failed.");
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return Task.FromResult(Unknown());
    }

    public void ReloadDatabase()
    {
        // Open the replacement before taking the write lock. If creation
        // fails, TryCreateReader returns null and the old reader survives.
        var replacement = TryCreateReader();

        if (replacement is null)
        {
            _logger.LogWarning(
                "GeoLite2 reload failed. Keeping the existing reader.");

            return;
        }

        try
        {
            _lock.EnterWriteLock();

            try
            {
                var previous = _reader;
                _reader = replacement;
                replacement = null;

                previous?.Dispose();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            // Disposes the replacement only if the swap did not occur.
            replacement?.Dispose();
        }
    }

    private DatabaseReader? TryCreateReader()
    {
        var path = Path.GetFullPath(_options.DatabasePath);

        if (!File.Exists(path))
        {
            _logger.LogWarning(
                "GeoLite2 database not found at {Path}.",
                path);

            return null;
        }

        try
        {
            // Loading into memory allows the underlying file to be replaced
            // without retaining an active memory-mapped file handle.
            return new DatabaseReader(path, FileAccessMode.Memory);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to open GeoLite2 database at {Path}.",
                path);

            return null;
        }
    }

    private static GeoLocationInfo Unknown() =>
        new("Unknown", "Unknown");

    private static bool IsNonPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address)
            || address.Equals(IPAddress.Any)
            || address.Equals(IPAddress.IPv6Any)
            || address.Equals(IPAddress.IPv6None)
            || address.Equals(IPAddress.IPv6Loopback))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return IsIPv4NonPublic(bytes);
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // IPv6 link-local, unique-local, multicast, and deprecated
            // site-local addresses.
            if (address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || address.IsIPv6Multicast)
            {
                return true;
            }

            // fc00::/7 — unique-local addresses.
            if ((bytes[0] & 0xFE) == 0xFC)
            {
                return true;
            }

            // 2001: db8::/32 — documentation addresses.
            if (bytes[0] == 0x20
                && bytes[1] == 0x01
                && bytes[2] == 0x0D
                && bytes[3] == 0xB8)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsIPv4NonPublic(byte[] bytes)
    {
        var a = bytes[0];
        var b = bytes[1];
        var c = bytes[2];

        return a == 0                              // 0.0.0.0/8
            || a == 10                             // 10.0.0.0/8
            || a == 100 && (b & 0xC0) == 0x40     // 100.64.0.0/10
            || a == 127                            // 127.0.0.0/8
            || a == 169 && b == 254                // 169.254.0.0/16
            || a == 172 && b is >= 16 and <= 31     // 172.16.0.0/12
            || a == 192 && b == 168                // 192.168.0.0/16
            || a == 192 && b == 0 && c == 0       // 192.0.0.0/24
            || a == 192 && b == 0 && c == 2       // 192.0.2.0/24
            || a == 192 && b == 88 && c == 99     // 192.88.99.0/24
            || a == 198 && b is 18 or 19          // 198.18.0.0/15
            || a == 198 && b == 51 && c == 100    // 198.51.100.0/24
            || a == 203 && b == 0 && c == 113     // 203.0.113.0/24
            || a >= 224;                           // Multicast/reserved
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _lock.EnterWriteLock();

        try
        {
            if (_disposed)
            {
                return;
            }

            _reader?.Dispose();
            _reader = null;
            _disposed = true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _lock.Dispose();
    }
}