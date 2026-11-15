namespace AnnihilatedRoomRetry;

internal sealed class RetryCheckpoint
{
    private bool capturing;
    private bool ready;
    public bool Loading { get; private set; }

    public bool BeginCapture()
    {
        if (Loading || capturing)
            return false;

        ready = false;
        capturing = true;
        return true;
    }

    public void FinishCapture(bool success)
    {
        if (!capturing)
            return;

        capturing = false;
        ready = success;
    }

    public bool CanRetry(bool inBattle) => inBattle && ready && !Loading;

    public bool BeginRetry(bool inBattle)
    {
        if (!CanRetry(inBattle))
            return false;

        Loading = true;
        return true;
    }

    public void FinishRetry() => Loading = false;

    public void Invalidate()
    {
        capturing = false;
        ready = false;
    }
}
