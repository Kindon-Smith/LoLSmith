namespace Services.Riot.Dtos;

public class InfoDto
{
    public long GameCreation { get; set; }
    public long GameDuration { get; set; }
    public long GameId { get; set; }
    public string? GameMode { get; set; }
    public string? GameType { get; set; }
    public string? GameVersion { get; set; }
    public int MapId { get; set; }
    public string? PlatformId { get; set; }
    public int QueueId { get; set; }
}