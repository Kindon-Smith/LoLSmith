using System.Collections.Concurrent;
using Services.Riot;
using LoLSmith.Db;

public interface IBackgroundFetchQueue
{
    void Enqueue(object job);
    bool TryDequeue(out object? job);
}

public class BackgroundFetchQueue : IBackgroundFetchQueue
{
    private readonly ConcurrentQueue<object> _q = new();
    public void Enqueue(object job) => _q.Enqueue(job);
    public bool TryDequeue(out object? job) => _q.TryDequeue(out job);
}

public record FetchUserRefreshJob(string Platform, string Name, string Tag);
public record FetchUserMatchesJob(string Platform, string Puuid);
public record FetchMatchDetailsJob(string Platform, string MatchId);