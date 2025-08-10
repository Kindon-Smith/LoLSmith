
using System.ComponentModel.DataAnnotations;

namespace Options.RiotOptions;

public class RiotOptions
{
    [Required] public string? ApiKey { get; set; }
}