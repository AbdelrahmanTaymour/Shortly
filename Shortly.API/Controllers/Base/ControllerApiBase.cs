using Microsoft.AspNetCore.Mvc;
using Shortly.Core.Exceptions.ClientErrors;

namespace Shortly.API.Controllers.Base;

/// <summary>
///     Base controller class providing common functionality for API controllers.
///     Includes user authentication, authorization, and claim extraction utilities.
/// </summary>
/// <remarks>
///     This base class should be inherited by all API controllers that require user authentication.
///     It provides secure methods to extract user information from JWT claims with proper validation
///     and error handling.
/// </remarks>
public abstract class ControllerApiBase : ControllerBase
{

    protected void ValidatePage(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new ValidationException("Page number must be greater than 0.");
        
        if (pageSize < 1 || pageSize > 100)
            throw new  ValidationException("Page size must be between 1 and 100.");
    }

    protected void ValidateDateRange(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new  ValidationException("Start date must be before or equal to end date.");
    }
}