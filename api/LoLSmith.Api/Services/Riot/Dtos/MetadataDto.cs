using LoLSmith.Db;

namespace Services.Riot.Dtos;

public class MetadataDto
{
    public string MatchId { get; set; } = string.Empty;
    public string DataVersion { get; set; } = string.Empty;

    // Riot returns an array of PUUID strings here, not objects
    public List<string> Participants { get; set; } = new List<string>();
}