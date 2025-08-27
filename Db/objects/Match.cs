namespace LoLSmith.Db;

public class Match
{
    public int Id { get; set; }
    public string? MatchId { get; set; }

    // From InfoDto
    public DateTime GameCreation { get; set; }
    public long GameDuration { get; set; }
    public string? GameMode { get; set; }
    public string? GameType { get; set; }
    public string? GameVersion { get; set; }
    public int MapId { get; set; }
    public string? PlatformId { get; set; }
    public int QueueId { get; set; }

    // New: for cache and background fetch
    public string? Platform { get; set; }
    public string? DetailsJson { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime InsertedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<UserMatches> UserMatches { get; set; } = new List<UserMatches>();
}