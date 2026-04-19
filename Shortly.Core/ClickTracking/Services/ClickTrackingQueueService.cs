using System.Collections.Concurrent;
using Shortly.Core.ClickTracking.DTOs;

namespace Shortly.Core.ClickTracking.Services;

public class ClickTrackingQueueService
{
    private readonly ConcurrentQueue<(long redirectId, Guid? shortUrlOwnerId, ClickTrackingData trackingData)> _clickQueue = new();
    private readonly SemaphoreSlim _signal = new(0);

    /// <summary>
    /// Enqueues a click tracking job for processing.
    /// </summary>
    /// <param name="shortUrlId">The ID of the redirect to track.</param>
    /// <param name="shortUrlOwnerId">Denormalized user Id to improve analytics performance</param>
    /// <param name="trackingData">The tracking data associated with the click.</param>
    public void EnqueueClick(long shortUrlId, Guid? shortUrlOwnerId, ClickTrackingData trackingData)
    {
        if (trackingData == null)
            throw new ArgumentNullException(nameof(trackingData));

        _clickQueue.Enqueue((shortUrlId, shortUrlOwnerId, trackingData));
        _signal.Release(); // Notify the background worker
    }

    /// <summary>
    /// Dequeues the next available click tracking job asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    public async Task<(long redirectId, Guid? shortUrlOwnerId, ClickTrackingData trackingData)?> DequeueClickAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);

        _clickQueue.TryDequeue(out var job);
        return job;
    }
}