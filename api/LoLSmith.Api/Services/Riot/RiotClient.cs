using Microsoft.Extensions.Options;
using Options.RiotOptions;
using Services.Riot;
using Services.Riot.Dtos;

public class RiotClient : IRiotClient, IRiotAccountClient
{

    private readonly HttpClient _http;
    private readonly IOptionsMonitor<RiotOptions> _options;
    public RiotClient(HttpClient http, IOptionsMonitor<RiotOptions> options)
    {
        _http = http;
        _options = options;

        _http.Timeout = TimeSpan.FromSeconds(10);
    }
    public async Task<SummonerDto?> GetSummonerByNameAsync(string platform, string name, CancellationToken ct = default)
    {
        var url = $"https://{platform}.api.riotgames.com/lol/summoner/v4/summoners/by-name/{Uri.EscapeDataString(name)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("X-Riot-Token", _options.CurrentValue.ApiKey);
        request.Headers.Accept.ParseAdd("application/json");

        var res = await _http.SendAsync(request, ct);
        VerifyStatusCode(res);

        var dto = await res.Content.ReadFromJsonAsync<SummonerDto>
            (new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct);

        return dto ?? throw new KeyNotFoundException("Summoner not found for the given name");
    }

    public async Task<PuuidDto?> GetPuuidByRiotIdAsync(string platform, string riotName, string riotTag, CancellationToken ct = default)
    {
        //https://americas.api.riotgames.com/riot/account/v1/accounts/by-riot-id/Blue/lim3
        var url = $"https://{platform}.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(riotName)}/{Uri.EscapeDataString(riotTag)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("X-Riot-Token", _options.CurrentValue.ApiKey);
        request.Headers.Accept.ParseAdd("application/json");

        var res = await _http.SendAsync(request, ct);
        VerifyStatusCode(res);

        var puuidDto = await res.Content.ReadFromJsonAsync<PuuidDto>
            (new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct);
        return puuidDto ?? throw new KeyNotFoundException("Puuid not found for the given Riot ID");
    }

    public void VerifyStatusCode(HttpResponseMessage res)
    {
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException("Summoner not found");
        }
        else if (res.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                 res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Invalid or Expired Riot API key");
        }
        else if (res.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException("Rate limited by Riot API");
        }
        else
        {
            res.EnsureSuccessStatusCode();
        }
    }
}