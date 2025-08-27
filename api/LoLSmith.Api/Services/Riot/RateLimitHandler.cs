using System.Net.Http;
using Microsoft.Extensions.Options;
using Options.RiotOptions;
using Utils;

public class RateLimitHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<RiotOptions> _options;
    private readonly ILogger<RateLimitHandler> _logger;

    private static readonly object _lockObject = new();

    private static double _tokens;
    private static double _capacity;
    private static double _fillRatePerSecond;
    private static DateTime _lastRefill = DateTime.UtcNow;

    public RateLimitHandler(IOptionsMonitor<RiotOptions> options, ILogger<RateLimitHandler> logger)
    {
        _options = options;
        _logger = logger;

        var maxPerMinute = Math.Max(1, _options.CurrentValue.RateLimit.MaxRequestsPerMinute);

        lock (_lockObject)
        {
            _capacity = maxPerMinute;
            _fillRatePerSecond = maxPerMinute / 60.0;
            if (_tokens == 0) _tokens = _capacity;
            _lastRefill = DateTime.UtcNow;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_options.CurrentValue.RateLimit.Enabled)
        {
            while (true)
            {
                double waitSeconds = 0;
                lock (_lockObject)
                {
                    var now = DateTime.UtcNow;
                    var elapsed = (now - _lastRefill).TotalSeconds;
                    if (elapsed > 0)
                    {
                        _tokens = Math.Min(_capacity, _tokens + _fillRatePerSecond * elapsed);
                        _lastRefill = now;
                    }

                    if (_tokens >= 1.0)
                    {
                        _tokens -= 1.0;
                        _logger.LogDebug($"{nameof(RateLimitHandler)}: token acquired, tokens remaining = {_tokens}");
                        break;
                    }

                    waitSeconds = (1.0 - _tokens) / _fillRatePerSecond;
                    if (waitSeconds < 0) waitSeconds = 0;
                }

                _logger.LogInformation($"{nameof(RateLimitHandler)}: rate limit reached. Waiting for {Math.Ceiling(waitSeconds)} seconds");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning($"{nameof(RateLimitHandler)}: request cancelled while waiting for rate limit");
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        var response = await base.SendAsync(request, cancellationToken);
        // handle 429 retry
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning($"{nameof(RateLimitHandler)}: rate limit exceeded, retrying request");
            var retryDelay = TimeSpan.FromSeconds(5); // Default retry delay
            if (response.Headers.TryGetValues("Retry-After", out var values) && int.TryParse(values.FirstOrDefault(), out var seconds))
            {
                // Handle retry
                retryDelay = TimeSpan.FromSeconds(seconds);
            }
            _logger.LogInformation($"{nameof(RateLimitHandler)}: honoring Retry-After header, waiting for {retryDelay.TotalSeconds} seconds");
            response.Dispose();
            try
            {
                await Task.Delay(retryDelay, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning($"{nameof(RateLimitHandler)}: request cancelled while waiting for retry delay");
                cancellationToken.ThrowIfCancellationRequested();
            }

            response = await base.SendAsync(request, cancellationToken);
        }
        ApiResponseValidator.VerifyStatusCode(response);
        return response;
    }
}