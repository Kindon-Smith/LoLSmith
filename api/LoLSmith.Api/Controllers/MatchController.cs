using Microsoft.AspNetCore.Mvc;
using LoLSmith.Db;
using Services.Riot;
using Microsoft.EntityFrameworkCore;
using Services.Riot.Dtos;
using LoLSmith.Api.Controllers.Mappers;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/matches")]
public class MatchController : ControllerBase
{
    private readonly LoLSmithDbContext _db;
    private readonly IRiotMatchClient _matches;
    private readonly IBackgroundFetchQueue _queue;

    public MatchController(LoLSmithDbContext db, IRiotMatchClient matches, IBackgroundFetchQueue queue)
    {
        _db = db; _matches = matches; _queue = queue;
    }

    // IDs by PUUID -> DB-first; refresh in background
    [Authorize]
    [HttpGet("{platform}/by-puuid/{puuid}")]
    public async Task<IActionResult> GetIdsByPuuid(string platform, string puuid, CancellationToken ct)
    {
        // return existing sorted by start time (denormalize or join)
        var existing = await _db.UserMatches
            .Where(um => um.User!.Puuid == puuid)
            .OrderByDescending(um => um.Match!.GameCreation) // ensure Match.GameCreation exists
            .Select(um => um.Match!.MatchId)
            .ToListAsync(ct);

        // fire-and-forget background refresh
        _queue.Enqueue(new FetchUserMatchesJob(platform, puuid));

        return Ok(existing);
    }

    // Details by matchId -> DB-first; refresh in background if missing/stale
    [Authorize]
    [HttpGet("{platform}/by-id/{matchId}")]
    public async Task<IActionResult> GetById(string platform, string matchId, CancellationToken ct)
    {
        var match = await _db.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId, ct);

        // If row missing OR details missing => enqueue and return 202
        if (match == null || string.IsNullOrWhiteSpace(match.DetailsJson))
        {
            _queue.Enqueue(new FetchMatchDetailsJob(platform, matchId));
            Response.Headers["Retry-After"] = "2";
            return Accepted(new { status = "fetching", matchId });
        }

        // If stale => return current and refresh in background
        if ((DateTime.UtcNow - match.LastUpdated) > TimeSpan.FromDays(7))
        {
            _queue.Enqueue(new FetchMatchDetailsJob(platform, matchId));
        }

        return Content(match.DetailsJson!, "application/json");
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

    [Authorize][HttpGet("debug/match/{matchId}")]
    public async Task<IActionResult> DebugGetMatch(string matchId)
    {
        var m = await _db.Matches
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.MatchId == matchId);
        if (m is null) return NotFound(new { message = "Match not found" });
        return Ok(m);
    }
}
