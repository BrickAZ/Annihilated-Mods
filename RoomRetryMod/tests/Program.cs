using AnnihilatedRoomRetry;

var checkpoint = new RetryCheckpoint();
Check(!checkpoint.CanRetry(true), "F9 requires a checkpoint from this session.");
Check(checkpoint.BeginCapture(), "The first battle can start a capture.");
Check(!checkpoint.CanRetry(true), "F9 cannot load a capture in progress.");
checkpoint.FinishCapture(true);
Check(checkpoint.CanRetry(true), "A successful capture enables F9 in this battle.");
Check(!checkpoint.CanRetry(false), "F9 is inactive outside battle.");
Check(checkpoint.BeginRetry(true), "F9 starts a reload.");
Check(!checkpoint.BeginRetry(true), "Repeated F9 cannot start a second reload.");
Check(!checkpoint.BeginCapture(), "Restoring the battle start cannot overwrite its checkpoint.");
checkpoint.FinishRetry();
Check(checkpoint.CanRetry(true), "The battle can be retried again.");
Check(checkpoint.BeginCapture(), "A new battle can replace the preceding checkpoint.");
checkpoint.FinishCapture(false);
Check(!checkpoint.CanRetry(true), "A failed capture cannot revive the previous battle's save.");
Check(checkpoint.BeginCapture(), "Capture can recover on the next battle.");
checkpoint.Invalidate();
checkpoint.FinishCapture(true);
Check(!checkpoint.CanRetry(true), "A pending capture cannot survive an unrelated game load.");
Console.WriteLine("Room-retry checkpoint tests passed.");

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
