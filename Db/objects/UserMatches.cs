namespace LoLSmith.Db;

public class UserMatches
{
    public int UserId { get; set; }
    public int MatchId { get; set; }
    public DateTime InsertedAt { get; set; } = DateTime.UtcNow;
}
