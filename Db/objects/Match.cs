namespace LoLSmith.Db;

public class Match
{
    public int Id { get; set; }

    public string? MatchId { get; set; }

    public ICollection<UserMatches> UserMatches { get; set; } = new List<UserMatches>();

    public DateTime InsertedAt { get; set; } = DateTime.UtcNow;
}