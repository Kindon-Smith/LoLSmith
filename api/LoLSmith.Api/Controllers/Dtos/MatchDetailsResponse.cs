namespace LoLSmith.Api.Controllers.Dtos;

public class MatchDetailsResponse
{
    public string MatchId { get; set; } = string.Empty;
    public DateTime GameCreationUtc { get; set; }
    public int GameDuration { get; set; }
    public string GameMode { get; set; } = string.Empty;
    public string GameType { get; set; } = string.Empty;
    public string GameVersion { get; set; } = string.Empty;
    public int MapId { get; set; }
    public string PlatformId { get; set; } = string.Empty;
    public int QueueId { get; set; }
    public List<string> Participants { get; set; } = new();
}