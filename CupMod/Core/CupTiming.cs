using System.Globalization;

namespace CupCompetition;

internal enum CupPauseReason
{
    None,
    Save,
    Cutscene,
    Preparing,
    Input,
    Skip,
    RoomRetry
}

internal static class CupTiming
{
    public static bool IsFixedWait(string? mode) =>
        float.TryParse(mode, NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds) &&
        float.IsFinite(seconds) && seconds > 0;

    public static CupPauseReason PauseFor(bool saving, bool lockedCutscene, bool canAct,
        bool preparing = false, bool input = false, bool skipActive = false, bool advancingDialogue = false,
        bool roomRetry = false)
    {
        if (preparing) return CupPauseReason.Preparing;
        if (roomRetry) return CupPauseReason.RoomRetry;
        if (saving) return CupPauseReason.Save;
        if (input) return CupPauseReason.Input;
        if (canAct) return CupPauseReason.None;
        if (skipActive && advancingDialogue) return CupPauseReason.Skip;
        return lockedCutscene ? CupPauseReason.Cutscene : CupPauseReason.None;
    }
}
