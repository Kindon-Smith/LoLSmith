using System.Net.Http;
using Microsoft.Extensions.Options;
using Options.RiotOptions;
using Utils;

public class RateLimitHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<RiotOptions> _options;
    private static int _requestCount = 0;
    private static DateTime _windowStart = DateTime.UtcNow;
    private static readonly object _lockObject = new();

    public RateLimitHandler(IOptionsMonitor<RiotOptions> options)
    {
        _options = options;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_options.CurrentValue.RateLimit.Enabled)
        {
            var maxRequests = _options.CurrentValue.RateLimit.MaxRequestsPerMinute;
            while (true)
            {
                TimeSpan waitTime = TimeSpan.Zero;
                lock (_lockObject)
                {
                    var now = DateTime.UtcNow;
                    var elapsed = now - _windowStart;
                    if (elapsed >= TimeSpan.FromMinutes(1))
                    {
                        _requestCount = 0;
                        _windowStart = now;
                    }
                    if (_requestCount < maxRequests)
                    {
                        _requestCount++;
                        break;
                    }
                    waitTime = TimeSpan.FromMinutes(1) - elapsed;
                    if (waitTime < TimeSpan.Zero) waitTime = TimeSpan.Zero;
                }
                await Task.Delay(waitTime, cancellationToken);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);
        ApiResponseValidator.VerifyStatusCode(response);
        return response;
    }
}