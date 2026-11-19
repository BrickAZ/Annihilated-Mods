using System.Text.Json;
using UnityEngine;

namespace CupCompetition;

public sealed partial class CupMod
{
    private CupAudit? audit;
    private byte[]? finalResult;
    private GUIStyle? exportButtonStyle;
    private string exportNotice = "";
    private bool exportSucceeded;
    private static Rect ExportButtonRect(Rect hud) => new(hud.x, hud.yMax + 4, Math.Min(hud.width, 205), 34);

    private static void WriteResult(string path, byte[] data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp";
        File.WriteAllBytes(temporary, data);
        File.Move(temporary, path, true);
    }

    private string ExportResult()
    {
        string directory = smoke ? Path.Combine(root, "verification", "exports") : Path.Combine(root, "exports");
        string path = Path.Combine(directory, $"{T("杯赛成绩", "Cup-Result")}-{runId.ToUpperInvariant()}.json");
        WriteResult(path, finalResult!);
        LoggerInstance.Msg($"Cup result exported | {path}");
        return path;
    }

    private void DrawResultExport(Rect hud)
    {
        if (exportButtonStyle == null)
        {
            exportButtonStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter, font = hudStyle!.font, fontSize = hudStyle.fontSize
            };
            exportButtonStyle.normal.background = GUI.skin.button.normal.background;
            exportButtonStyle.hover.background = GUI.skin.button.hover.background;
            exportButtonStyle.active.background = GUI.skin.button.active.background;
            exportButtonStyle.normal.textColor = exportButtonStyle.hover.textColor = exportButtonStyle.active.textColor = Color.white;
        }
        if (GUI.Button(ExportButtonRect(hud), T("导出比赛成绩", "Export Cup Result"), exportButtonStyle))
        {
            try
            {
                ExportResult();
                exportSucceeded = true;
                exportNotice = T($"已导出 {audit!.RunCode}", $"Exported {audit!.RunCode}") + " · UserData/AnnihilatedCup/exports";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                exportSucceeded = false;
                exportNotice = T("导出失败，请检查游戏目录写入权限后重试。", "Export failed. Check write permissions for the game folder and try again.");
                LoggerInstance.Warning($"Cup result export failed: {ex.Message}");
            }
        }
        if (exportNotice.Length == 0) return;
        var notice = new Rect(hud.x, hud.yMax + 42, hud.width, 34);
        GUI.Box(notice, "");
        GUI.Label(notice, "  " + exportNotice, hudStyle!);
    }

    private System.Collections.IEnumerator VerifyExportButtonForSmoke()
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 5;
        while (exportButtonStyle == null && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        if (exportButtonStyle == null)
        {
            // A hidden Unity player does not dispatch OnGUI. Still exercise its normal file-writing path.
            LoggerInstance.Msg("Cup export smoke: hidden player did not render the button; checking result export directly.");
            try { VerifyExportForSmoke("clear", new[] { "start", "timer_start", "finish" }, 0); }
            catch (Exception ex) { Fail(ex); }
            yield break;
        }
        var button = ExportButtonRect(GetHudRect());
        Event.QueueEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = button.center });
        yield return null;
        Event.QueueEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = button.center });
        deadline = Time.realtimeSinceStartupAsDouble + 3;
        while (exportNotice.Length == 0 && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
        if (!exportSucceeded)
        {
            Fail(new InvalidOperationException("The in-game export button did not acknowledge its click."));
            yield break;
        }
        try { VerifyExportForSmoke("clear", new[] { "start", "timer_start", "finish" }, 0); }
        catch (Exception ex) { Fail(ex); }
    }

    private void VerifyExportForSmoke(string expectedStatus, string[] expectedEvents, long expectedPenalty)
    {
        string exported = ExportResult();
        // Changes to the live game after the finish must never leak into an exported run.
        string native = Path.Combine(root, "verification", runId + ".json");
        byte[] original = File.ReadAllBytes(native);
        if (exported != ExportResult() || !original.AsSpan().SequenceEqual(File.ReadAllBytes(exported)))
            throw new InvalidOperationException("Repeated export did not reproduce the saved result.");
        using var document = JsonDocument.Parse(original);
        var result = document.RootElement;
        var events = result.GetProperty("Events").EnumerateArray().ToArray();
        if (!result.GetProperty("RunCode").ValueEquals(audit!.RunCode) ||
            !Path.GetFileName(exported).Contains(audit.RunCode, StringComparison.Ordinal) ||
            !result.GetProperty("Status").ValueEquals(expectedStatus) ||
            !expectedEvents.SequenceEqual(events.Where(evt => !evt.GetProperty("Kind").ValueEquals("checkpoint"))
                .Select(evt => evt.GetProperty("Kind").GetString())) ||
            events.Sum(evt => evt.GetProperty("PenaltyAddedMilliseconds").GetInt64()) != expectedPenalty ||
            result.GetProperty("PenaltyTotalMilliseconds").GetInt64() != expectedPenalty ||
            !events[^1].GetProperty("Result").ValueEquals(expectedStatus) ||
            (expectedStatus == "clear" && (!result.GetProperty("CompletedGoal").ValueEquals(GoalId) ||
                !events[^1].GetProperty("Detail").ValueEquals(GoalId))))
            throw new InvalidOperationException("Exported result and event timeline did not match the completed run.");
        LoggerInstance.Msg($"Cup export smoke passed | code={audit.RunCode} | {expectedStatus} | {expectedPenalty}ms penalty");
    }
}
