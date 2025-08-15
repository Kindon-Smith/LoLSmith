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
    
    // Navigation properties
    public ICollection<UserMatches> UserMatches { get; set; } = new List<UserMatches>();
    
    public DateTime InsertedAt { get; set; } = DateTime.UtcNow;
}