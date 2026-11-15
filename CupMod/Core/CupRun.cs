using System.Diagnostics;
using System.Text;
namespace CupCompetition;

internal sealed class CupRun
{
    private readonly Stopwatch timer = new();
    private bool started;
    private bool retryPending;
    public int Progress { get; private set; }
    public int RoomRetries { get; private set; }
    public int Deaths { get; private set; }
    public TimeSpan RoomRetryPenalty { get; private set; }
    public TimeSpan DeathPenalty { get; private set; }
    public TimeSpan Penalty => RoomRetryPenalty + DeathPenalty;
    public string? Result { get; private set; }
    public bool Running => started && Result == null;
    public bool Paused => Running && !timer.IsRunning;
    public bool AllowsRoomRetryLoad => Running && retryPending;
    public TimeSpan Elapsed => timer.Elapsed + Penalty;

    public bool TryBeginRoomRetry(int limit)
    {
        if (!Running || retryPending || RoomRetries >= limit) return false;
        retryPending = true;
        return true;
    }

    public bool CompleteRoomRetry(bool success, int penaltySeconds)
    {
        if (!retryPending) return false;
        retryPending = false;
        if (!success || !Running) return false;
        RoomRetries++;
        RoomRetryPenalty += TimeSpan.FromSeconds(penaltySeconds);
        return true;
    }

    public bool RecordDeath(int limit, int penaltySeconds)
    {
        if (!Running) return false;
        Deaths++;
        DeathPenalty += TimeSpan.FromSeconds(penaltySeconds);
        return Deaths <= limit;
    }
    public void Start()
    {
        if (started) return;
        started = true;
        timer.Start();
    }

    public void SetPaused(bool paused)
    {
        if (!Running) return;
        if (paused) timer.Stop();
        else timer.Start();
    }

    public void Reach(int rank)
    {
        if (Running) Progress = Math.Max(Progress, rank);
    }
    public void Finish(string result)
    {
        if (!Running) return;
        timer.Stop();
        retryPending = false;
        Result = result;
    }
}

internal static class CupRandom
{
    public static int Seed(int seed, string domain, params int[] coordinates)
    {
        uint hash = unchecked((uint)seed) ^ 2166136261;
        foreach (byte b in Encoding.UTF8.GetBytes(domain)) hash = unchecked((hash ^ b) * 16777619);
        foreach (int n in coordinates)
            for (int shift = 0; shift < 32; shift += 8)
                hash = unchecked((hash ^ (byte)(n >> shift)) * 16777619);
        return unchecked((int)hash);
    }
}
