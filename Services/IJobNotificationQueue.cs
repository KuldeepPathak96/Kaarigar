using System.Threading.Channels;

namespace Kaarigar.Services;

/// <summary>
/// Lightweight in-memory queue so "Post Job" (Screen E-03) can return
/// immediately to the employer instead of blocking on WhatsApp fan-out to
/// every matching employee. A single background worker
/// (JobNotificationBackgroundService) drains this and does the actual
/// matching + sending.
///
/// NOTE: in-memory only — queued items are lost on app restart/crash. That's
/// an acceptable trade-off while WhatsAppNotificationService is itself a
/// stub (no real provider wired in yet); swap this for a durable queue (a
/// DB-backed outbox table, Azure Storage Queue, etc.) once a real WhatsApp
/// Business API integration goes live and delivery guarantees matter.
/// </summary>
public interface IJobNotificationQueue
{
    /// <summary>Enqueues a just-created/updated JobPostId for match+notify processing.</summary>
    void QueueJobPost(int jobPostId);

    /// <summary>Consumed only by JobNotificationBackgroundService.</summary>
    IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken);
}

public class JobNotificationQueue : IJobNotificationQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

    public void QueueJobPost(int jobPostId) => _channel.Writer.TryWrite(jobPostId);

    public IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
