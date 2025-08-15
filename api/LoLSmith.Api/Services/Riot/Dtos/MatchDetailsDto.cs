using Services.Riot.Dtos;
namespace Services.Riot.Dtos;

public class MatchDetailsDto
{
    public MetadataDto Metadata { get; set; } = new MetadataDto();
    
    public InfoDto Info { get; set; } = new InfoDto();
}