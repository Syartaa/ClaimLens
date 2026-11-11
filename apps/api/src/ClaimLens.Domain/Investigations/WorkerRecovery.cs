namespace ClaimLens.Domain.Investigations;

public static class WorkerLimits
{
    public const int MaxAttempts = 3;

    public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(2);

    public const string InterruptedCode = "WORKER_INTERRUPTED";

    public const string CancelledCode = "CANCELLED";
}

public enum RecoveryAction
{
    None,
    Requeue,
    Fail
}

public static class WorkerRecovery
{
    public static RecoveryAction Decide(ProcessingStatus status, DateTimeOffset updatedAt, int attemptCount, DateTimeOffset now)
    {
        if (status is ProcessingStatus.Pending or ProcessingStatus.Completed or ProcessingStatus.Failed)
        {
            return RecoveryAction.None;
        }

        if (updatedAt >= now - WorkerLimits.StaleAfter)
        {
            return RecoveryAction.None;
        }

        return attemptCount >= WorkerLimits.MaxAttempts ? RecoveryAction.Fail : RecoveryAction.Requeue;
    }
}
