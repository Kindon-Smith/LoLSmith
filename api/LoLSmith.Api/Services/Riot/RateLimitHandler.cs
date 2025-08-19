using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Options.RiotOptions;
using Utils;

public class RateLimitHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<RiotOptions> _options;
    private readonly object _lock = new();
    private double _tokens;
    private DateTime _lastRefill = DateTime.UtcNow;

    public RateLimitHandler(IOptionsMonitor<RiotOptions> options)
    {
        _options = options;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cfg = _options.CurrentValue.RateLimit;
        if (!cfg.Enabled) return await base.SendAsync(request, cancellationToken);

        // token-bucket params
        var maxPerMinute = Math.Max(1, cfg.MaxRequestsPerMinute);
        var maxTokens = maxPerMinute; // allow bursting up to the minute budget
        var refillPerSecond = maxPerMinute / 60.0;
        var maxQueueDelaySeconds = cfg is RateLimitWithQueueOptions q ? q.MaxQueueDelaySeconds : 5; // default small limit

        // try acquire token or wait a small time
        while (true)
        {
            double waitSeconds = 0;
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var elapsed = (now - _lastRefill).TotalSeconds;
                if (elapsed > 0)
                {
                    _tokens = Math.Min(maxTokens, _tokens + elapsed * refillPerSecond);
                    _lastRefill = now;
                }

                if (_tokens >= 1.0)
                {
                    _tokens -= 1.0;
                    waitSeconds = 0;
                    break;
                }

                // time until next token is available
                waitSeconds = (1.0 - _tokens) / refillPerSecond;
            }

            if (waitSeconds <= 0) continue;

            if (waitSeconds > maxQueueDelaySeconds)
            {
                // don't block forever — reject so caller can implement retry/backoff
                var resp = new HttpResponseMessage((HttpStatusCode)429) // Too Many Requests
                {
                    ReasonPhrase = "Rate limit (token bucket) - request rejected instead of queued"
                };
                return resp;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
        }

        // send and validate
        var response = await base.SendAsync(request, cancellationToken);

        // if Riot returns Retry-After or 429, consider honoring it (could set internal next window)
        // let ApiResponseValidator still handle status code semantics
        ApiResponseValidator.VerifyStatusCode(response);

        return response;
    }

    // optional typed options subclass to configure the max queue delay
    public class RateLimitWithQueueOptions : RiotOptions.RateLimitOptions
    {
        public int MaxQueueDelaySeconds { get; set; } = 5;
    }
}