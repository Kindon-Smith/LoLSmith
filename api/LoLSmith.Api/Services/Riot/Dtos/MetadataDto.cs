using LoLSmith.Db;

namespace Services.Riot.Dtos;

public class MetadataDto
{
    public string DataVersion { get; set; } = string.Empty;
    public string MatchId { get; set; } = string.Empty;

    public ICollection<ParticipantDto> Participants { get; set; } = new List<ParticipantDto>();
}