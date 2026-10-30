using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Extensions;
using Shortly.Core.Exceptions.DTOs;
using Shortly.Core.Users.DeleteAccount;
using Shortly.Core.Users.GetLoggedInUser;
using Shortly.Core.Users.GetMyProfile;
using Shortly.Core.Users.GetMyQuotaStatus;
using Shortly.Core.Users.UpdateProfile;
using Shortly.Domain.Users;
using Shortly.Infrastructure.Authorization;

namespace Shortly.API.Controllers.Users;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize]
[RequiredPermission(enPermissions.ManageOwnAccount)]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpGet("me", Name = "GetLoggedInUser")]
    [ProducesResponseType(typeof(LoggedInUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLoggedInUser(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetLoggedInUserQuery(), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }
    
    [HttpGet("profile", Name = "GetMyProfile")]
    [ProducesResponseType(typeof(MyProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMyProfileQuery(), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPut("profile", Name = "UpdateMyProfile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var command = new UpdateProfileCommand(
            request.Name, request.Bio, request.PhoneNumber, request.Website,
            request.Company, request.Location, request.Country, request.TimeZone);

        var result = await sender.Send(command, cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok();
    }
    
    [HttpDelete(Name = "RequestAccountDeletion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestAccountDeletion(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DeleteAccountCommand(), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok();
    }

    [HttpGet("usage", Name = "GetMyQuotaStatus")]
    [ProducesResponseType(typeof(QuotaStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyQuotaStatus(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMyQuotaStatusQuery(), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }
}

