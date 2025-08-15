namespace Services.Riot.Dtos;

public class MetadataDto
{
    public string DataVersion { get; set; } = string.Empty;
    public string MatchId { get; set; } = string.Empty;

    public List<string> Participants { get; set; } = new List<string>();
}