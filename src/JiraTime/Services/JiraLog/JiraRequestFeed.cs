using System.Threading.Channels;
using JiraTime.Domain;

namespace JiraTime.Services;

public enum JiraRequestStage { Started, Finished }

/// <summary>A Jira request going out (<see cref="JiraRequestStage.Started"/>) or ending, as the live feed tells it.</summary>
public sealed record JiraRequestEvent(JiraRequestStage Stage, JiraLogEntry Entry);

/// <summary>
/// Live Jira requests per user, for the toasts and the Logs page. Each subscriber gets its own queue that
/// is read in order on a background task, so a slow or failing subscriber never holds up a request or
/// another subscriber. Subscribers are called off the UI thread and marshal to their component themselves.
/// </summary>
public sealed class JiraRequestFeed(ILogger<JiraRequestFeed> logger)
{
    /// <summary>Events kept for a subscriber that falls behind; older ones are dropped.</summary>
    private const int QueueSize = 500;

    private readonly Lock _lock = new();
    private readonly Dictionary<string, List<Subscription>> _byOwner = [];

    /// <summary>Calls <paramref name="listener"/> with every request of <paramref name="owner"/> until disposed.</summary>
    public IDisposable Subscribe(string owner, Func<JiraRequestEvent, Task> listener)
    {
        var subscription = new Subscription(this, owner, listener);
        lock (_lock)
        {
            if (!_byOwner.TryGetValue(owner, out var list)) _byOwner[owner] = list = [];
            list.Add(subscription);
        }
        return subscription;
    }

    /// <summary>Hands the event to the owner's subscribers without waiting for them.</summary>
    public void Publish(string owner, JiraRequestEvent e)
    {
        Subscription[] subscribers;
        lock (_lock)
        {
            if (!_byOwner.TryGetValue(owner, out var list)) return;
            subscribers = [.. list];
        }
        foreach (var subscriber in subscribers) subscriber.Post(e);
    }

    private void Remove(Subscription subscription)
    {
        lock (_lock)
        {
            if (!_byOwner.TryGetValue(subscription.Owner, out var list)) return;
            list.Remove(subscription);
            if (list.Count == 0) _byOwner.Remove(subscription.Owner);
        }
    }

    private void Report(Exception ex) => logger.LogWarning(ex, "A Jira request feed subscriber failed.");

    private sealed class Subscription : IDisposable
    {
        private readonly JiraRequestFeed _feed;
        private readonly Func<JiraRequestEvent, Task> _listener;
        private readonly Channel<JiraRequestEvent> _queue = Channel.CreateBounded<JiraRequestEvent>(
            new BoundedChannelOptions(QueueSize) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

        public string Owner { get; }

        public Subscription(JiraRequestFeed feed, string owner, Func<JiraRequestEvent, Task> listener)
        {
            (_feed, Owner, _listener) = (feed, owner, listener);
            _ = Task.Run(DeliverAsync);
        }

        public void Post(JiraRequestEvent e) => _queue.Writer.TryWrite(e);

        private async Task DeliverAsync()
        {
            await foreach (var e in _queue.Reader.ReadAllAsync())
            {
                try
                {
                    await _listener(e);
                }
                catch (Exception ex)
                {
                    _feed.Report(ex);
                }
            }
        }

        public void Dispose()
        {
            _feed.Remove(this);
            _queue.Writer.TryComplete();
        }
    }
}
