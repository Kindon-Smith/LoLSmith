using Services.Riot.Dtos;
using Utils;

namespace Services.Riot;
public interface IRiotMatchClient
{
    Task<MatchListDto?> GetMatchesByPuuidAsync(string platform, string puuid, CancellationToken ct = default);

    Task<MatchDetailsDto?> GetMatchDetailsByIdAsync(string platform, string matchId, CancellationToken ct = default);
}
