using ClaimLens.Domain.Investigations;

namespace ClaimLens.Domain.Tests;

public class WorkerRecoveryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Fresh_in_progress_work_is_left_alone()
    {
        var action = WorkerRecovery.Decide(ProcessingStatus.ExtractingClaims, Now.AddMinutes(-1), 1, Now);

        Assert.Equal(RecoveryAction.None, action);
    }

    [Fact]
    public void Stale_work_is_requeued_before_the_attempt_cap()
    {
        var action = WorkerRecovery.Decide(ProcessingStatus.GatheringEvidence, Now.AddMinutes(-3), 2, Now);

        Assert.Equal(RecoveryAction.Requeue, action);
    }

    [Fact]
    public void Stale_work_fails_after_three_attempts()
    {
        var action = WorkerRecovery.Decide(ProcessingStatus.AnalyzingEvidence, Now.AddMinutes(-3), WorkerLimits.MaxAttempts, Now);

        Assert.Equal(RecoveryAction.Fail, action);
    }

    [Fact]
    public void Pending_and_finished_rows_are_not_recovered()
    {
        Assert.Equal(RecoveryAction.None, WorkerRecovery.Decide(ProcessingStatus.Pending, Now.AddHours(-1), 1, Now));
        Assert.Equal(RecoveryAction.None, WorkerRecovery.Decide(ProcessingStatus.Completed, Now.AddHours(-1), 1, Now));
        Assert.Equal(RecoveryAction.None, WorkerRecovery.Decide(ProcessingStatus.Failed, Now.AddHours(-1), 1, Now));
    }
}
