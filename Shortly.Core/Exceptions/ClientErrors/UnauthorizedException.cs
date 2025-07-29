using System.Net;
using Shortly.Core.Exceptions.Base;

namespace Shortly.Core.Exceptions.ClientErrors;

/// <summary>
///     Exception thrown when user is not authenticated
/// </summary>
public sealed class UnauthorizedException : ClientErrorException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;

    public UnauthorizedException() : base("Authentication is required to access this resource.")
    {
    }

    public UnauthorizedException(string message) : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}