using Shortly.Domain.Abstractions;

namespace Shortly.Infrastructure.Email;

internal interface IEmailSender
{
    Task<Result> SendAsync(EmailRequest request, CancellationToken cancellationToken = default);
}