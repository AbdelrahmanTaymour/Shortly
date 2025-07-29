using System.Net;
using Shortly.Core.Exceptions.Base;

namespace Shortly.Core.Exceptions.ServerErrors;

/// <summary>
///     Exception thrown when a dependent or external service is temporarily unavailable.
/// </summary>
public sealed class ServiceUnavailableException : ServerErrorException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.ServiceUnavailable;

    public ServiceUnavailableException(string serviceName)
        : base($"Service '{serviceName}' is currently unavailable.", new { ServiceName = serviceName })
    {
    }

    public ServiceUnavailableException(string serviceName, Exception innerException)
        : base($"Service '{serviceName}' is currently unavailable.", innerException, new { ServiceName = serviceName })
    {
    }
}