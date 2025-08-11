using Services.Riot.Dtos;

namespace Services.Riot;

public interface IRiotAccountClient
{
    Task<PuuidDto?> GetPuuidByRiotIdAsync(string platform, string riotName, string riotTag, CancellationToken ct = default);
}