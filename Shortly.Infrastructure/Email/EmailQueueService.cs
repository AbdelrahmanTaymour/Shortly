using System.Collections.Concurrent;
using Shortly.Core.Abstractions.Email;

namespace Shortly.Infrastructure.Email;

/// <summary>
/// Provides a thread-safe queuing mechanism for managing and processing email requests.
/// </summary>
internal sealed class EmailQueueService
{
    private readonly ConcurrentQueue<EmailRequest> _emailQueue = new();
    private readonly SemaphoreSlim _signal = new(0);
    
    public void EnqueueEmail(EmailRequest emailRequest)
    {
        ArgumentNullException.ThrowIfNull(emailRequest);

        _emailQueue.Enqueue(emailRequest);
        _signal.Release();
    }

    public async Task<EmailRequest?> DequeueEmailAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);

        _emailQueue.TryDequeue(out var emailRequest);
        return emailRequest;
    }
}