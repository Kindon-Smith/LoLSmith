using Microsoft.AspNetCore.Mvc;
using LoLSmith.Db;
using Services.Riot;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/summoners")]
public class SummonerController : ControllerBase
{
    private readonly LoLSmithDbContext _db;
    private readonly IRiotAccountClient _accounts;
    private readonly IBackgroundFetchQueue _queue;

    public SummonerController(LoLSmithDbContext db, IRiotAccountClient accounts, IBackgroundFetchQueue queue)
    {
        _db = db; _accounts = accounts; _queue = queue;
    }

    [Authorize]
    [HttpGet("{platform}/{name}/{tag}")]
    public async Task<IActionResult> GetByRiotId(string platform, string name, string tag, CancellationToken ct)
    {
        var nName = name.Trim().ToLowerInvariant();
        var nTag = tag.Trim().ToLowerInvariant();

        // DB-first
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.SummonerName!.ToLower() == nName
                                   && u.TagLine!.ToLower() == nTag, ct);

        var stale = user != null && (DateTime.UtcNow - user.LastUpdated) > TimeSpan.FromHours(24);

        if (user != null)
        {
            if (stale)
            {
                // refresh in background (non-blocking)
                _queue.Enqueue(new FetchUserRefreshJob(platform, name, tag));
            }
            return Ok(new { puuid = user.Puuid, gameName = user.SummonerName, tagLine = user.TagLine });
        }

        // cache miss -> call Riot once
        var acct = await _accounts.GetPuuidByRiotIdAsync(platform, name, tag, ct);
        if (acct == null) return NotFound();

        // upsert
        var entity = new User
        {
            Puuid = acct.Puuid,
            SummonerName = acct.GameName,
            TagLine = acct.TagLine,
            LastUpdated = DateTime.UtcNow
        };
        _db.Users.Add(entity);
        await _db.SaveChangesAsync(ct);

        // optional: kick off match prefetch
        _queue.Enqueue(new FetchUserMatchesJob(platform, acct.Puuid!));

        return Ok(new { puuid = acct.Puuid, gameName = acct.GameName, tagLine = acct.TagLine });
    }
}