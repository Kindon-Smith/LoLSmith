
using System.ComponentModel.DataAnnotations;

namespace Options.RiotOptions;

public class RiotOptions
{
    [Required] public string? ApiKey { get; set; }
    [Required] public RateLimitOptions RateLimit { get; set; } = new RateLimitOptions();

    public class RateLimitOptions
    {
        public bool Enabled { get; set; } = true;
        public int MaxRequestsPerMinute { get; set; } = 20;

    }
}