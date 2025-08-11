using Services.Riot.Dto;
using Services.Riot.Dtos;
using Utils;

namespace Services.Riot;

public interface IRiotAccountClient
{
    Task<RiotAccountDto?> GetPuuidByRiotIdAsync(string platform, string riotName, string riotTag, CancellationToken ct = default);
}