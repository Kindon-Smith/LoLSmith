namespace LoLSmith.Db;

public class Match
{
    public int Id { get; set; }

    public string? MatchId { get; set; }

    public List<string> Participants { get; set; } = new List<string>();

    public DateTime InsertedAt { get; set; } = DateTime.UtcNow;
}