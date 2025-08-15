namespace LoLSmith.Db;

public class User
{
    public int Id { get; set; }
    public string? Puuid { get; set; }
    public string? SummonerName { get; set; }
    public string? TagLine { get; set; }

    public ICollection<UserMatches> UserMatches { get; set; } = new List<UserMatches>();
    public DateTime LastUpdated { get; set; }
}