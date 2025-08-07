using System.Net;
using Shortly.Core.Exceptions.Base;

namespace Shortly.Core.Exceptions.ClientErrors;

/// <summary>
/// Exception thrown when a requested resource is not found
/// </summary>
public sealed class NotFoundException : ClientErrorException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with identifier '{key}' was not found.", new { EntityName = entityName, Key = key })
    {
    }
}