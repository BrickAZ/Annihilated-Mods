using System.Diagnostics;

namespace CupCompetition;

internal sealed record CupAuditEvent(
    int Sequence, string Kind, string? Result, string? Detail, long WallMilliseconds,
    long ElapsedMilliseconds, long PenaltyAddedMilliseconds, long PenaltyTotalMilliseconds,
    int RoomRetriesUsed, int Deaths);

internal sealed class CupAudit
{
    private readonly Stopwatch wallClock = Stopwatch.StartNew();
    private readonly List<CupAuditEvent> events = new();

    public string RunCode { get; }
    public IReadOnlyList<CupAuditEvent> Events => events;

    public CupAudit(string runId) => RunCode = runId[(runId.LastIndexOf('-') + 1)..].ToUpperInvariant();

    public void Record(CupRun run, string kind, string? result = null, int penaltySeconds = 0, string? detail = null)
    {
        events.Add(new CupAuditEvent(events.Count + 1, kind, result, detail, wallClock.ElapsedMilliseconds,
            (long)run.Elapsed.TotalMilliseconds, penaltySeconds * 1000L, (long)run.Penalty.TotalMilliseconds,
            run.RoomRetries, run.Deaths));
    }
}
