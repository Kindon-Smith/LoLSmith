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
            GameMode = info.GameMode,
            GameType = info.GameType,
            GameVersion = info.GameVersion,
            MapId = info.MapId,
            PlatformId = info.PlatformId,
            QueueId = info.QueueId,
            Participants = dto.Metadata?.Participants?.ToList() ?? new List<string>()
        };
    }
}