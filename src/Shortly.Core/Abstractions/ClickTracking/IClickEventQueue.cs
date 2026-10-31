namespace Shortly.Core.Abstractions.ClickTracking;

public interface IClickEventQueue
{
    void Enqueue(ClickEventCapture capture);
    IAsyncEnumerable<ClickEventCapture> DequeueAllAsync(CancellationToken cancellationToken);
}