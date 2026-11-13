namespace RoushTech.Myotis.Smtp;

using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Options;
using RoushTech.Myotis.Settings;

// Remembers failed SMTP sign-ins per address and blocks an address that keeps guessing.
public class SignInGuard(IOptionsMonitor<LimitsOptions> limits, TimeProvider time)
{
    private const int MaxTrackedAddresses = 100_000;

    private readonly ConcurrentDictionary<IPAddress, Tracker> trackers = new();

    public bool IsBlocked(IPAddress address) =>
        trackers.TryGetValue(address, out var tracker) && tracker.BlockedUntil(time.GetUtcNow()) is not null;

    public bool RecordFailure(IPAddress address)
    {
        var now = time.GetUtcNow();
        if (trackers.Count >= MaxTrackedAddresses)
        {
            Prune(now);
        }

        var current = limits.CurrentValue;
        return trackers.GetOrAdd(address, _ => new Tracker()).Fail(
            now,
            current.BlockAfterFailures,
            TimeSpan.FromMinutes(current.BlockWindowMinutes),
            TimeSpan.FromMinutes(current.BlockMinutes));
    }

    private void Prune(DateTimeOffset now)
    {
        var window = TimeSpan.FromMinutes(limits.CurrentValue.BlockWindowMinutes);
        foreach (var (address, tracker) in trackers)
        {
            if (tracker.IsIdle(now, window))
            {
                trackers.TryRemove(address, out _);
            }
        }
    }

    private sealed class Tracker
    {
        private readonly Lock gate = new();
        private readonly Queue<DateTimeOffset> failures = new();
        private DateTimeOffset? blockedUntil;

        public DateTimeOffset? BlockedUntil(DateTimeOffset now)
        {
            lock (gate)
            {
                return blockedUntil > now ? blockedUntil : null;
            }
        }

        public bool Fail(DateTimeOffset now, int threshold, TimeSpan window, TimeSpan blockFor)
        {
            lock (gate)
            {
                failures.Enqueue(now);
                while (failures.Count > 0 && failures.Peek() <= now - window)
                {
                    failures.Dequeue();
                }

                if (failures.Count >= threshold)
                {
                    blockedUntil = now + blockFor;
                    failures.Clear();
                }

                return blockedUntil > now;
            }
        }

        public bool IsIdle(DateTimeOffset now, TimeSpan window)
        {
            lock (gate)
            {
                return !(blockedUntil > now) && (failures.Count == 0 || failures.Last() <= now - window);
            }
        }
    }
}
