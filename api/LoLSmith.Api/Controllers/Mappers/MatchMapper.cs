using Services.Riot.Dtos;
using LoLSmith.Api.Controllers.Dtos;

namespace LoLSmith.Api.Controllers.Mappers;

public static class MatchMapper
{
    public static MatchDetailsResponse ToResponse(this MatchDetailsDto dto)
    {
        var info = dto.Info;
        return new MatchDetailsResponse
        {
            MatchId = dto.Metadata?.MatchId ?? string.Empty,
            GameCreationUtc = DateTimeOffset.FromUnixTimeMilliseconds(info.GameCreation).UtcDateTime,
            GameDuration = (int)info.GameDuration,
            GameMode = info.GameMode ?? string.Empty,
            GameType = info.GameType ?? string.Empty,
            GameVersion = info.GameVersion ?? string.Empty,
            MapId = info.MapId,
            PlatformId = info.PlatformId ?? string.Empty,
            QueueId = info.QueueId,
            Participants = dto.Metadata?.Participants?.Select(p => p.Puuid).ToList()
        };
    }
}