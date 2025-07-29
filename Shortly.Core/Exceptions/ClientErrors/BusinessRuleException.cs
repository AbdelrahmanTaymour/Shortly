using System.Net;
using Shortly.Core.Exceptions.Base;

namespace Shortly.Core.Exceptions.ClientErrors;

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public sealed class BusinessRuleException : ClientErrorException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;

    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string rule, string violation)
        : base($"The business rule '{rule}' was violated: {violation}.", new { Rule = rule, Violation = violation })
    {
    }
}