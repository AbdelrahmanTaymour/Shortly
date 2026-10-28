using System.Threading.Channels;
using Shortly.Core.Abstractions.ClickTracking;

namespace Shortly.Infrastructure.ClickTracking;

internal sealed class ClickEventQueue : IClickEventQueue
{
    private readonly Channel<ClickEventCapture> _channel = Channel.CreateBounded<ClickEventCapture>(
        new BoundedChannelOptions(10_000) { FullMode = BoundedChannelFullMode.DropOldest });

    public void Enqueue(ClickEventCapture capture) => _channel.Writer.TryWrite(capture);

    public IAsyncEnumerable<ClickEventCapture> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}