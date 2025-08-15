namespace LoLSmith.Db;

public class UserMatches
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public DateTime InsertedAt { get; set; } = DateTime.UtcNow;
}
