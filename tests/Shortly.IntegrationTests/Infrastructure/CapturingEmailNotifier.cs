using System.Collections.Concurrent;
using Shortly.Core.Abstractions.Email;

namespace Shortly.IntegrationTests.Infrastructure;

/// <summary>The only external service Shortly has is email: tests record what would have been sent instead of sending it.</summary>
public sealed class CapturingEmailNotifier : IEmailNotifier
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> Sent => _sent.ToArray();

    public void EnqueueSendEmailVerificationAsync(string email, string userName, string verificationToken) =>
        _sent.Enqueue(new SentEmail("EmailVerification", email, verificationToken));

    public void EnqueuePasswordResetAsync(string email, string userName, string resetToken) =>
        _sent.Enqueue(new SentEmail("PasswordReset", email, resetToken));

    public void EnqueueSendUserInvitation(string email, string inviterUsername, string inviteeName, string invitationToken, string organizationName) =>
        _sent.Enqueue(new SentEmail("Invitation", email, invitationToken));

    public void EnqueueEmailChangeConfirmationAsync(string newEmail, string username, string changeToken) =>
        _sent.Enqueue(new SentEmail("EmailChange", newEmail, changeToken));
}

public sealed record SentEmail(string Kind, string To, string Token);
