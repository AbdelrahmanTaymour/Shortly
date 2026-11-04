using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shortly.Api.Extensions;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.ChangePassword;
using Shortly.Core.Account.ConfirmEmailChange;
using Shortly.Core.Account.ForgotPassword;
using Shortly.Core.Account.InitiateEmailChange;
using Shortly.Core.Account.ResetPassword;
using Shortly.Core.Account.SendEmailVerification;
using Shortly.Core.Account.ValidateResetToken;
using Shortly.Core.Account.VerifyEmail;
using Shortly.Core.Exceptions;
using Shortly.Infrastructure.EdgeProtection.Models;

namespace Shortly.Api.Controllers.Account;

[ApiController]
[Route("api/account")]
[Produces("application/json")]
public class AccountController(ISender sender) : ControllerBase
{
    [HttpPost("send-email-verification", Name = "Account_SendEmailVerification")]
    [EnableRateLimiting(AccountEmailRateLimitOptions.PolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendEmailVerification(
        [FromBody] SendEmailVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new SendEmailVerificationCommand(request.Email);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    [HttpPost("verify-email", Name = "Account_VerifyEmail")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new VerifyEmailCommand(request.Token);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPost("change-email", Name = "Account_ChangeEmail")]
    [EnableRateLimiting(AccountEmailRateLimitOptions.PolicyName)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangeEmail(
        [FromBody] ChangeEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new InitiateEmailChangeCommand(request.NewEmail, request.Password);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    [HttpPost("confirm-email-change", Name = "Account_ConfirmEmailChange")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmEmailChange(
        [FromBody] ConfirmEmailChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ConfirmEmailChangeCommand(request.Token);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    [HttpPost("forgot-password", Name = "Account_ForgotPassword")]
    [EnableRateLimiting(AccountEmailRateLimitOptions.PolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);

        return NoContent();
    }

    [HttpPost("validate-reset-token", Name = "Account_ValidateResetToken")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateResetToken(
        [FromBody] ValidateResetTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ValidateResetTokenCommand(request.Token), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpPost("reset-password", Name = "Account_ResetPassword")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }

    [HttpPost("change-password", Name = "Account_ChangePassword")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : NoContent();
    }
}