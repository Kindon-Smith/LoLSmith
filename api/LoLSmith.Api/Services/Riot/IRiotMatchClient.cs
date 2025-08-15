using Services.Riot.Dtos;
using Utils;

public interface IRiotMatchClient
{
    Task<MatchListDto?> GetMatchesByPuuidAsync(string platform, string puuid, CancellationToken ct = default);

    Task<MatchDetailsDto?> GetMatchDetailsByIdAsync(string platform, string matchId, CancellationToken ct = default);
}
