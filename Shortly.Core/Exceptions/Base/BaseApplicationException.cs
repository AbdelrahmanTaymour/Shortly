using System.Net;

namespace Shortly.Core.Exceptions.Base;

public abstract class BaseApplicationException : Exception
{
    public abstract HttpStatusCode StatusCode { get; }
    public virtual string ErrorCode => GetType().Name.Replace("Exception", "");
    public virtual object? Details { get; }

    protected BaseApplicationException(string message) : base(message)
    {
    }

    protected BaseApplicationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected BaseApplicationException(string message, object? details) : base(message)
    {
        Details = details;
    }
}