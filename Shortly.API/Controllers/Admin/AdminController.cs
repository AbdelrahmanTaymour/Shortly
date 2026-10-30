using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Extensions;
using Shortly.Core.Admin.BulkActivateUsers;
using Shortly.Core.Admin.BulkDeactivateUsers;
using Shortly.Core.Admin.BulkDeleteUsers;
using Shortly.Core.Admin.CreateUser;
using Shortly.Core.Admin.ForceUpdateUser;
using Shortly.Core.Admin.GetUserDetail;
using Shortly.Core.Admin.HardDeleteUser;
using Shortly.Core.Admin.LockUser;
using Shortly.Core.Admin.ResetAllUsersUsage;
using Shortly.Core.Admin.ResetUserUsage;
using Shortly.Core.Admin.SearchUsers;
using Shortly.Core.Admin.UnlockUser;
using Shortly.Core.Exceptions.DTOs;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;
using Shortly.Infrastructure.Authorization;

namespace Shortly.API.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Produces("application/json")]
[Authorize]
public class AdminController(ISender sender) : ControllerBase
{
    [HttpGet("search", Name = "SearchUsers")]
    [RequiredPermission(enPermissions.ViewUsers)]
    [ProducesResponseType(typeof(PagedResponse<UserAdminSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] SearchUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        var result =
            await sender.Send(
                new SearchUsersQuery(request.Search, request.Status, request.Role, request.Page, request.PageSize),
                cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("{userId:guid}", Name = "GetUserDetail")]
    [RequiredPermission(enPermissions.ViewUserDetails)]
    [ProducesResponseType(typeof(UserAdminDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetUserDetailQuery(userId, null, null), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("by-email", Name = "GetUserByEmail")]
    [RequiredPermission(enPermissions.ViewUserDetails)]
    [ProducesResponseType(typeof(UserAdminDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEmail([FromQuery] string email, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetUserDetailQuery(null, email, null), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }
    
    [HttpGet("by-username", Name = "GetUserByUsername")]
    [RequiredPermission(enPermissions.ViewUserDetails)]
    [ProducesResponseType(typeof(UserAdminDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUsername([FromQuery] string username, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetUserDetailQuery(null, null, username), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPost(Name = "AdminCreateUser")]
    [RequiredPermission(enPermissions.CreateUser)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] AdminCreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var result =
            await sender.Send(new CreateUserCommand(request.Email, request.Username, request.Password, request.Role),
                cancellationToken);
        return result.IsFailure
            ? result.ToProblemDetails()
            : CreatedAtAction(nameof(GetById), new { userId = result.Value }, null);
    }

    [HttpPatch("{userId:guid}", Name = "ForceUpdateUser")]
    [RequiredPermission(enPermissions.UpdateUser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ForceUpdate(Guid userId, [FromBody] ForceUpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ForceUpdateUserCommand(
            userId, request.Email, request.Username, request.Role, request.SubscriptionPlanId, request.IsActive,
            request.ProfileName, request.ProfileBio, request.ProfilePhoneNumber,
            request.ProfileCompany, request.ProfileLocation, request.ProfileCountry, request.ProfileTimeZone);

        var result = await sender.Send(command, cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }


    [HttpDelete("{userId:guid}", Name = "HardDeleteUser")]
    [RequiredPermission(enPermissions.DeleteUser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> HardDelete(Guid userId, [FromQuery] bool deleteOwnedShortUrls = false,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new HardDeleteUserCommand(userId, deleteOwnedShortUrls), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    [HttpPost("bulk/activate", Name = "BulkActivateUsers")]
    [RequiredPermission(enPermissions.ReactivateUser)]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkActivate([FromBody] BulkUserIdsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new BulkActivateUsersCommand(request.UserIds), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPost("bulk/deactivate", Name = "BulkDeactivateUsers")]
    [RequiredPermission(enPermissions.DeactivateUser)]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkDeactivate([FromBody] BulkUserIdsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new BulkDeactivateUsersCommand(request.UserIds), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpDelete("bulk", Name = "BulkDeleteUsers")]
    [RequiredPermission(enPermissions.DeleteUser)]
    [ProducesResponseType(typeof(BulkOperationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkDelete([FromBody] BulkUserIdsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new BulkDeleteUsersCommand(request.UserIds), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }
    
    [HttpPost("{userId:guid}/usage/reset", Name = "ResetUserUsage")]
    [RequiredPermission(enPermissions.ManageUserUsage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetUsage(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ResetUserUsageCommand(userId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok();
    }

    [HttpPost("usage/reset-all", Name = "ResetAllUsersUsage")]
    [RequiredPermission(enPermissions.ManageOrgUsage)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetAllUsage(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ResetAllUsersUsageCommand(), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(new { usersAffected = result.Value });
    }

    [HttpPost("{userId:guid}/lock", Name = "LockUser")]
    [RequiredPermission(enPermissions.ManageUserAccessibility)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Lock(Guid userId, [FromBody] LockUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new LockUserCommand(userId, request.Reason, request.Duration),
            cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok();
    }

    [HttpPost("{userId:guid}/unlock", Name = "UnlockUser")]
    [RequiredPermission(enPermissions.ManageUserAccessibility)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlock(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new UnlockUserCommand(userId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok();
    }
}