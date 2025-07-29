using System.Net;
using Shortly.Core.Exceptions.Base;

namespace Shortly.Core.Exceptions.ServerErrors;

/// <summary>
///     Exception thrown when a required configuration value is missing or invalid.
/// </summary>
public sealed class ConfigurationException : ServerErrorException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.InternalServerError;

    public ConfigurationException(string configKey)
        : base($"Configuration key '{configKey}' is missing or invalid.", new { ConfigurationKey = configKey })
    {
    }

    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}