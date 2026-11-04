using System.Net;

namespace Shortly.Infrastructure.EdgeProtection.Services;

/// <summary>
/// Creates a unique key for rate-limiting based on the client's IP address (IPv4 or IPv6 network).
/// </summary>
public static class ClientIpPartitionKey
{
    public static string From(IPAddress? address)
    {
        if (address is null) return "unknown";

        // Convert IPv4 addresses wrapped in IPv6 format back to regular IPv4
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        
        // For IPv4, use the full IP address string
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) 
            return address.ToString();

        // For IPv6, group users by their network block (/64 prefix).
        // This stops attackers from faking millions of different IPs to bypass rate limits.
        Span<byte> bytes = stackalloc byte[16];
        return address.TryWriteBytes(bytes, out _)
            ? $"{Convert.ToHexString(bytes[..8])}/64"
            : address.ToString();
    }
}
