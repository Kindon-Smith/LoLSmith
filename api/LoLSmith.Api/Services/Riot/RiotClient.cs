using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using Options.RiotOptions;
using Services.Riot;
using Services.Riot.Dtos;
using Utils;

namespace Services.Riot;

public class RiotClient : IRiotAccountClient, IRiotMatchClient
{

    private readonly HttpClient _http;
    private readonly IOptionsMonitor<RiotOptions> _options;

    private static DateTime _windowStart = DateTime.UtcNow;
    private static readonly object _lockObject = new();
    public RiotClient(HttpClient http, IOptionsMonitor<RiotOptions> options)
    {
        _http = http;
        _options = options;
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<RiotAccountDto?> GetPuuidByRiotIdAsync(string platform, string riotName, string riotTag, CancellationToken ct = default)
    {
        //https://americas.api.riotgames.com/riot/account/v1/accounts/by-riot-id/Blue/lim3
        var url = $"https://{platform}.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(riotName)}/{Uri.EscapeDataString(riotTag)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("X-Riot-Token", _options.CurrentValue.ApiKey);
        request.Headers.Accept.ParseAdd("application/json");

        var res = await _http.SendAsync(request, ct);

        var riotAccountDto = await res.Content.ReadFromJsonAsync<RiotAccountDto>
            (new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct);
        return riotAccountDto ?? throw new KeyNotFoundException("Riot Account not found for the given Riot ID");
    }

    public async Task<MatchListDto?> GetMatchesByPuuidAsync(string platform, string puuid, CancellationToken ct = default)
    {
        var url = $"https://{platform}.api.riotgames.com/lol/match/v5/matches/by-puuid/{puuid}/ids";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("X-Riot-Token", _options.CurrentValue.ApiKey);
        request.Headers.Accept.ParseAdd("application/json");

        var res = await _http.SendAsync(request, ct);
        ApiResponseValidator.VerifyStatusCode(res);

        var matches = await res.Content.ReadFromJsonAsync<List<string>>
            (new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

        // Wrap in your DTO if needed
        return matches is null ? null : new MatchListDto { Matches = matches };
    }

    public async Task<MatchDetailsDto?> GetMatchDetailsByIdAsync(string platform, string matchId, CancellationToken ct = default)
    {
        var url = $"https://{platform}.api.riotgames.com/lol/match/v5/matches/{matchId}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("X-Riot-Token", _options.CurrentValue.ApiKey);
        request.Headers.Accept.ParseAdd("application/json");

        var res = await _http.SendAsync(request, ct);
        ApiResponseValidator.VerifyStatusCode(res);

        return await res.Content.ReadFromJsonAsync<MatchDetailsDto>
            (new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
    }

}