using System.Net;

namespace Shortly.Infrastructure.EdgeProtection.Models;

/// <summary>
/// Configuration settings for trusting reverse proxies (like Nginx or Cloudflare) 
/// so the app can see the real client IP address instead of the proxy's IP.
/// </summary>
public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>List of trusted proxy IP addresses.</summary>
    public string[] KnownProxies { get; init; } = [];

    /// <summary>List of trusted proxy networks in CIDR format (e.g., "10.0.0.0/8").</summary>
    public string[] KnownNetworks { get; init; } = [];

    /// <summary>
    /// If true, trusts forwarded headers from any sender. 
    /// Only use this if your app is hidden behind a secure hosting platform proxy. Cannot be used with KnownProxies or KnownNetworks.
    /// </summary>
    public bool TrustAllProxies { get; init; }

    /// <summary>Number of proxy hops to trust (defaults to 1).</summary>
    public int ForwardLimit { get; init; } = 1;

    /// <summary>
    /// Checks that settings are valid and throws an error immediately if there are configuration mistakes.
    /// </summary>
    public void Validate()
    {
        if (ForwardLimit < 1)
            throw new InvalidOperationException($"{SectionName}:ForwardLimit must be at least 1.");

        if (TrustAllProxies && (KnownProxies.Length > 0 || KnownNetworks.Length > 0))
            throw new InvalidOperationException(
                $"{SectionName}:TrustAllProxies cannot be combined with {SectionName}:KnownProxies or {SectionName}:KnownNetworks. " +
                "Configure either trusted proxies/networks or TrustAllProxies, not both.");

        foreach (var proxy in KnownProxies)
            if (!IPAddress.TryParse(proxy, out _))
                throw new InvalidOperationException($"{SectionName}:KnownProxies contains an invalid IP address: '{proxy}'.");

        foreach (var network in KnownNetworks)
            ParseNetwork(network);
    }

    /// <summary>
    /// Parses a CIDR network string (like "10.0.0.0/8") into an IPNetwork object.
    /// </summary>
    public static Microsoft.AspNetCore.HttpOverrides.IPNetwork ParseNetwork(string network)
    {
        var parts = network.Split('/');
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var address) || !int.TryParse(parts[1], out var prefix) ||
            prefix < 0 || prefix > (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128))
            throw new InvalidOperationException(
                $"{SectionName}:KnownNetworks contains an invalid network: '{network}'. Use CIDR notation such as 10.0.0.0/8.");

        return new Microsoft.AspNetCore.HttpOverrides.IPNetwork(address, prefix);
    }
}