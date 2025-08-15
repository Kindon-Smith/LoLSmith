namespace LoLSmith.Db;

public class ParticipantDto
{
    public string Puuid { get; set; } = null!;

    public string? SummonerName { get; set; }

    public int ChampionId { get; set; }

    public int TeamId { get; set; }

    public int Kills { get; set; }

    public int Deaths { get; set; }

    public int Assists { get; set; }

    public int GoldEarned { get; set; }

    public int TotalDamageDealtToChampions { get; set; }

    public int TotalDamageTaken { get; set; }

    public bool IsWinner { get; set; }
}