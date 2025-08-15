using Microsoft.AspNetCore.Mvc;
using LoLSmith.Db;
using Services.Riot;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/summoners")]
public class SummonerController : ControllerBase
{
    private readonly IRiotAccountClient _accounts;
    private readonly LoLSmithDbContext _db;
    private static readonly string[] allowedPlatforms = ["americas", "europe", "asia"];

    public SummonerController(IRiotAccountClient accounts, LoLSmithDbContext db)
    {
        _accounts = accounts;
        _db = db;
    }

    [HttpGet("{platform}/{name}/{tag}")]
    // /api/summoners/{region}/{name}/{tag}

    public async Task<IActionResult> GetPuuidByRiotId(string platform, string name, string tag, CancellationToken ct)
    {

        // validate platform
        if (!allowedPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Invalid platform code." });
        }

        // call to client (regional host e.g., americas/europe/asia)
        var dto = await _accounts.GetPuuidByRiotIdAsync(platform, name, tag, ct);

        // 404 mapping
        if (dto is null) return NotFound();

        // Upsert lightweight user record based on PUUID
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Puuid == dto.Puuid, ct);
        if (existing is null)
        {
            _db.Users.Add(new User
            {
                Puuid = dto.Puuid,
                SummonerName = dto.GameName,
                TagLine = dto.TagLine,
                LastUpdated = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }
        else if (existing.SummonerName != dto.GameName || existing.TagLine != dto.TagLine)
        {
            existing.SummonerName = dto.GameName;
            existing.TagLine = dto.TagLine;
            existing.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { dto.Puuid, dto.GameName, dto.TagLine });
    }
}