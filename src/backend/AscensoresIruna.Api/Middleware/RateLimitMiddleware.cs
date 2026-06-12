using System.Collections.Concurrent;

namespace AscensoresIruna.Api.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly int _maxRequests;
    private readonly TimeSpan _window;

    private static readonly ConcurrentDictionary<string, LinkedList<DateTime>> _entries = new();

    public RateLimitMiddleware(RequestDelegate next, int maxRequests = 100, int windowMinutes = 1)
    {
        _next = next;
        _maxRequests = maxRequests;
        _window = TimeSpan.FromMinutes(windowMinutes);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == HttpMethods.Get &&
            context.Request.Path.StartsWithSegments("/api"))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            if (ip is not null)
            {
                var now = DateTime.UtcNow;
                var cutoff = now - _window;

                var timestamps = _entries.GetOrAdd(ip, _ => new LinkedList<DateTime>());

                lock (timestamps)
                {
                    while (timestamps.Count > 0 && timestamps.First!.Value < cutoff)
                        timestamps.RemoveFirst();

                    if (timestamps.Count >= _maxRequests)
                    {
                        context.Response.StatusCode = 429;
                        context.Response.ContentType = "application/json";
                        var retryAfter = (int)(_window.TotalSeconds) - (int)(now - timestamps.First!.Value).TotalSeconds;
                        context.Response.Headers["Retry-After"] = retryAfter.ToString();
                        return;
                    }

                    timestamps.AddLast(now);
                }
            }
        }

        await _next(context);
    }

    public static void Purge()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _entries)
        {
            lock (kvp.Value)
            {
                while (kvp.Value.Count > 0 && kvp.Value.First!.Value < now - TimeSpan.FromMinutes(5))
                    kvp.Value.RemoveFirst();

                if (kvp.Value.Count == 0)
                    _entries.TryRemove(kvp.Key, out _);
            }
        }
    }
}