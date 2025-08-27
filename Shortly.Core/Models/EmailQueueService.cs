using System.Collections.Concurrent;

namespace Shortly.Core.Models;

/// <summary>
/// Provides a thread-safe queuing mechanism for managing and processing email requests.
/// </summary>
/// <remarks>
/// This service uses a <see cref="ConcurrentQueue{T}"/> to store pending email requests 
/// and a <see cref="SemaphoreSlim"/> to coordinate producers (enqueueing emails) and 
/// consumers (dequeue emails). It ensures safe concurrent access across multiple threads 
/// and allows consumers to wait asynchronously when the queue is empty.
/// </remarks>
public class EmailQueueService
{
    private readonly ConcurrentQueue<EmailRequest> _emailQueue = new();
    private readonly SemaphoreSlim _signal = new(0);
    
    /// <summary>
    /// Adds an email request to the queue for later processing.
    /// </summary>
    /// <param name="emailRequest">
    /// The <see cref="EmailRequest"/> to enqueue. Must not be <c>null</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="emailRequest"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// Each call signals the internal worker that a new item is available.
    /// </remarks>
    public void EnqueueEmail(EmailRequest emailRequest)
    {
        if (emailRequest == null)
            throw new ArgumentNullException(nameof(emailRequest));

        _emailQueue.Enqueue(emailRequest);
        _signal.Release(); // Notify the worker to process the queue
    }

    /// <summary>
    /// Removes and returns the next email request from the queue, waiting asynchronously if none are available.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel the wait operation.
    /// </param>
    /// <returns>
    /// The next <see cref="EmailRequest"/> in the queue, or <c>null</c> if the queue is empty after waiting.
    /// </returns>
    /// <remarks>
    /// This method blocks asynchronously until an item becomes available in the queue or the operation is canceled.
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the <paramref name="cancellationToken"/> is canceled while waiting.
    /// </exception>
    public async Task<EmailRequest?> DequeueEmailAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);

        _emailQueue.TryDequeue(out var emailRequest);
        return emailRequest;
    }
}