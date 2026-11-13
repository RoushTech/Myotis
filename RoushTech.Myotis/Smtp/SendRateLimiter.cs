namespace RoushTech.Myotis.Smtp;

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using RoushTech.Myotis.Settings;

// A token bucket per credential: a minute's worth of messages can arrive in a burst, then they refill steadily.
public class SendRateLimiter(IOptionsMonitor<LimitsOptions> limits, TimeProvider time)
{
    private readonly ConcurrentDictionary<string, Bucket> buckets = new(StringComparer.OrdinalIgnoreCase);

    public bool TryAcquire(string credential)
    {
        var perMinute = limits.CurrentValue.MessagesPerMinute;
        return buckets.GetOrAdd(credential, _ => new Bucket(perMinute, time.GetUtcNow())).TryTake(perMinute, time.GetUtcNow());
    }

    private sealed class Bucket(double tokens, DateTimeOffset updated)
    {
        private readonly Lock gate = new();

        public bool TryTake(int perMinute, DateTimeOffset now)
        {
            lock (gate)
            {
                tokens = Math.Min(perMinute, tokens + ((now - updated).TotalMinutes * perMinute));
                updated = now;
                if (tokens < 1)
                {
                    return false;
                }

                tokens -= 1;
                return true;
            }
        }
    }
}
