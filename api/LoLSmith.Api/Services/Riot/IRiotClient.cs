using Services.Riot.Dtos;

namespace Services.Riot;

public interface IRiotClient
{
    Task<SummonerDto?> GetSummonerByNameAsync(string platform, string name, CancellationToken ct = default);
}