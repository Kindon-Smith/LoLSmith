using Microsoft.AspNetCore.Mvc;
using LoLSmith.Db;
using Services.Riot;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/matches")]

public class MatchController : ControllerBase
{
    private readonly IRiotMatchClient _matchClient;
    private readonly LoLSmithDbContext _db;
    private static readonly string[] allowedPlatforms = ["americas", "europe", "asia"];

    public MatchController(IRiotMatchClient matchClient, LoLSmithDbContext db)
    {
        _matchClient = matchClient;
        _db = db;
    }

    [HttpGet("{platform}/{puuid}")]
    public async Task<IActionResult> GetMatchesByPuuid(string platform, string puuid, CancellationToken ct)
    {
        // validate platform
        if (!allowedPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Invalid platform code." });
        }

        // call to client (regional host e.g., americas/europe/asia)
        var matchListDto = await _matchClient.GetMatchesByPuuidAsync(platform, puuid, ct);

        // 404 mapping
        if (matchListDto is null) return NotFound();

        foreach (var matchId in matchListDto.Matches)
        {
            // Upsert lightweight match record based on Match ID
            var existingMatch = await _db.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId, ct);
            if (existingMatch is null)
            {
                _db.Matches.Add(new Match
                {
                    MatchId = matchId,
                    InsertedAt = DateTime.UtcNow
                });
                _db.UserMatches.Add(new UserMatches
                {
                    UserId = puuid, // Assuming puuid is used as UserId for simplicity
                    MatchId = matchId,
                    InsertedAt = DateTime.UtcNow
                });
            }
            else
            {
                existingMatch.InsertedAt = DateTime.UtcNow;
            }
        }

        return Ok(matchListDto);
    }
}
