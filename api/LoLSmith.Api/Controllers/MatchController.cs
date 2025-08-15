using Microsoft.AspNetCore.Mvc;
using LoLSmith.Db;
using Services.Riot;
using Microsoft.EntityFrameworkCore;
using Services.Riot.Dtos;

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

        // Ensure user exists (lookup by puuid string)
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Puuid == puuid, ct);
        if (user is null)
        {
            user = new User { Puuid = puuid, LastUpdated = DateTime.UtcNow };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct); // get user.Id
        }

        var externalMatchIds = matchListDto.Matches; // List<string>
        if (externalMatchIds == null || externalMatchIds.Count == 0)
        {
            return Ok(new { message = "No matches found for this PUUID." });
        }

        // Load existing Match rows by external MatchId (BATCH OPERATION)
        var existingMatches = await _db.Matches
            .Where(m => externalMatchIds.Contains(m.MatchId!))
            .ToDictionaryAsync(m => m.MatchId!, ct);

        // Create missing Match rows (BATCH OPERATION)
        var missingMatchIds = externalMatchIds.Where(id => !existingMatches.ContainsKey(id)).ToList();
        if (missingMatchIds.Count > 0)
        {
            var newMatches = missingMatchIds.Select(id => new Match 
            { 
                MatchId = id, 
                InsertedAt = DateTime.UtcNow 
            }).ToList();
            
            _db.Matches.AddRange(newMatches);
            await _db.SaveChangesAsync(ct); // populate Match.Id values
            
            // Add new matches to dictionary
            foreach (var match in newMatches)
            {
                existingMatches[match.MatchId!] = match;
            }
        }

        // Now link user -> match using integer PKs, avoid duplicates (BATCH OPERATION)
        var userId = user.Id;
        var matchDbIds = existingMatches.Values.Select(m => m.Id).ToList();

        var existingUserMatchIds = await _db.UserMatches
            .Where(um => um.UserId == userId && matchDbIds.Contains(um.MatchId))
            .Select(um => um.MatchId)
            .ToListAsync(ct);

        var newUserMatches = existingMatches.Values
            .Where(m => !existingUserMatchIds.Contains(m.Id))
            .Select(m => new UserMatches 
            { 
                UserId = userId, 
                MatchId = m.Id, 
                InsertedAt = DateTime.UtcNow 
            })
            .ToList();

        if (newUserMatches.Count > 0)
        {
            _db.UserMatches.AddRange(newUserMatches);
            await _db.SaveChangesAsync(ct);
        }

        return Ok(matchListDto);
    }

    [HttpGet("{platform}/details/{matchId}")]
    public async Task<IActionResult> GetMatchDetailsById(string platform, string matchId, CancellationToken ct = default)
    {
        // validate platform
        if (!allowedPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Invalid platform code." });
        }

        // call to client (regional host e.g., americas/europe/asia)
        var matchDetailsDto = await _matchClient.GetMatchDetailsByIdAsync(platform, matchId, ct);

        // 404 mapping
        if (matchDetailsDto is null) return NotFound();

        return Ok(matchDetailsDto);
    }

    [HttpGet("debug/tables")]
    public async Task<IActionResult> GetTableInfo()
    {
        try 
        {
            var matchCount = await _db.Matches.CountAsync();
            var userCount = await _db.Users.CountAsync();
            var userMatchCount = await _db.UserMatches.CountAsync();
            
            return Ok(new 
            { 
                TablesExist = true,
                Matches = matchCount,
                Users = userCount,
                UserMatches = userMatchCount,
                DatabaseLocation = "c:\\Users\\kindo\\Projects\\LoLApp\\Db\\LoLSmith.db"
            });
        }
        catch (Exception ex)
        {
            return Ok(new 
            { 
                TablesExist = false,
                Error = ex.Message
            });
        }
    }
}
