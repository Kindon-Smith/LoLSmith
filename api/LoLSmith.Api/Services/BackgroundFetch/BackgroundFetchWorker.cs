using Microsoft.Extensions.Hosting;
using Services.Riot;
using LoLSmith.Db;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

public class BackgroundFetchWorker : BackgroundService
{
    private readonly IBackgroundFetchQueue _queue;
    private readonly IServiceProvider _services;
    private readonly ILogger<BackgroundFetchWorker> _log;

    public BackgroundFetchWorker(
        IBackgroundFetchQueue queue,
        IServiceProvider services,
        ILogger<BackgroundFetchWorker> log)
    {
        _queue = queue;
        _services = services;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var job) && job is not null)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<LoLSmithDbContext>();
                    var accounts = scope.ServiceProvider.GetRequiredService<IRiotAccountClient>();
                    var matches = scope.ServiceProvider.GetRequiredService<IRiotMatchClient>();
                    await Handle(job, db, accounts, matches, stoppingToken);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Background job failed: {Job}", job);
                }
            }
            else
            {
                await Task.Delay(250, stoppingToken);
            }
        }
    }

    private async Task Handle(
        object job,
        LoLSmithDbContext _db,
        IRiotAccountClient _accounts,
        IRiotMatchClient _matches,
        CancellationToken ct)
    {
        switch (job)
        {
            case FetchUserRefreshJob(var platform, var name, var tag):
                var acct = await _accounts.GetPuuidByRiotIdAsync(platform, name, tag, ct);
                if (acct?.Puuid is { } acctPuuid)
                {
                    var u = await _db.Users.FirstOrDefaultAsync(x => x.Puuid == acctPuuid, ct);
                    if (u != null) { u.LastUpdated = DateTime.UtcNow; await _db.SaveChangesAsync(ct); }
                }
                break;

            case FetchUserMatchesJob(var platform, var puuid):
                var list = await _matches.GetMatchesByPuuidAsync(platform, puuid, ct);
                if (list?.Matches is { } ids && ids.Count > 0)
                {
                    foreach (var id in ids)
                    {
                        // upsert match shell and link to user
                        var m = await _db.Matches.FirstOrDefaultAsync(x => x.MatchId == id, ct);
                        if (m == null)
                        {
                            m = new Match { MatchId = id, Platform = platform, LastUpdated = DateTime.MinValue };
                            _db.Matches.Add(m);
                        }
                        var user = await _db.Users.FirstAsync(x => x.Puuid == puuid, ct);
                        if (!await _db.UserMatches.AnyAsync(um => um.UserId == user.Id && um.MatchId == m.Id, ct))
                        {
                            _db.UserMatches.Add(new UserMatches { UserId = user.Id, Match = m });
                        }

                        // enqueue detail fetch
                        _queue.Enqueue(new FetchMatchDetailsJob(platform, id));
                    }
                    await _db.SaveChangesAsync(ct);
                }
                break;

            case FetchMatchDetailsJob(var platform, var matchId):
        {
            var details = await _matches.GetMatchDetailsByIdAsync(platform, matchId, ct);
            if (details == null) break;

            var entity = await _db.Matches.SingleOrDefaultAsync(x => x.MatchId == matchId, ct);
            if (entity == null)
            {
                entity = new Match { MatchId = matchId };
                _db.Matches.Add(entity);
            }

            entity.DetailsJson = JsonSerializer.Serialize(details);
            // Riot timestamps are usually unix ms
            var createdUtc = DateTimeOffset.FromUnixTimeMilliseconds(details.Info.GameCreation).UtcDateTime;
            entity.GameCreation = createdUtc;
            entity.LastUpdated = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            break;
        }
        }
    }
}