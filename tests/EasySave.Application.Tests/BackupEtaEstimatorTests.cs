using EasySave.State;

namespace EasySave.Application.Tests;

public class BackupEtaEstimatorTests
{
    [Fact]
    public void UpdateEstimate_ShouldUseFractionOfTotalFilesForWarmup()
    {
        var estimator = new BackupEtaEstimator();
        var t0 = new DateTime(2026, 2, 23, 10, 0, 0, DateTimeKind.Utc);

        // 7% of 100 files => 7 warmup files (with >=15s already satisfied).
        estimator.UpdateEstimate(1, BackupStatus.Active, 100, 100, 10_000, 10_000, t0);
        estimator.UpdateEstimate(1, BackupStatus.Active, 100, 99, 10_000, 9_900, t0.AddSeconds(3));
        estimator.UpdateEstimate(1, BackupStatus.Active, 100, 98, 10_000, 9_800, t0.AddSeconds(6));
        estimator.UpdateEstimate(1, BackupStatus.Active, 100, 97, 10_000, 9_700, t0.AddSeconds(9));
        estimator.UpdateEstimate(1, BackupStatus.Active, 100, 96, 10_000, 9_600, t0.AddSeconds(12));
        var beforeFractionTarget = estimator.UpdateEstimate(1, BackupStatus.Active, 100, 95, 10_000, 9_500, t0.AddSeconds(15));
        var atFractionTarget = estimator.UpdateEstimate(1, BackupStatus.Active, 100, 94, 10_000, 9_400, t0.AddSeconds(18));

        Assert.Null(beforeFractionTarget.EstimatedRemainingTime);
        Assert.NotNull(atFractionTarget.EstimatedRemainingTime);
    }

    [Fact]
    public void UpdateEstimate_ShouldReturnNullDuringWarmup_ThenEtaAfterWarmupFiles()
    {
        var estimator = new BackupEtaEstimator();
        var t0 = new DateTime(2026, 2, 23, 10, 0, 0, DateTimeKind.Utc);

        // total: 12 files / 1200 bytes => 100 bytes per file
        // for small jobs, warmup is mostly governed by warmup time (15s).
        var s0 = estimator.UpdateEstimate(1, BackupStatus.Active, 12, 12, 1200, 1200, t0);
        var s1 = estimator.UpdateEstimate(1, BackupStatus.Active, 12, 11, 1200, 1100, t0.AddSeconds(2));
        var s2 = estimator.UpdateEstimate(1, BackupStatus.Active, 12, 10, 1200, 1000, t0.AddSeconds(4));
        var s3 = estimator.UpdateEstimate(1, BackupStatus.Active, 12, 9, 1200, 900, t0.AddSeconds(6));
        var s4 = estimator.UpdateEstimate(1, BackupStatus.Active, 12, 8, 1200, 800, t0.AddSeconds(8));
        var s5 = estimator.UpdateEstimate(1, BackupStatus.Active, 12, 7, 1200, 700, t0.AddSeconds(10));
        var s6 = estimator.UpdateEstimate(1, BackupStatus.Active, 12, 6, 1200, 600, t0.AddSeconds(12));
        var s7 = estimator.UpdateEstimate(1, BackupStatus.Active, 12, 5, 1200, 500, t0.AddSeconds(14));
        var snapshot = estimator.UpdateEstimate(1, BackupStatus.Active, 12, 4, 1200, 400, t0.AddSeconds(16));

        Assert.Null(s0.EstimatedRemainingTime);
        Assert.Null(s1.EstimatedRemainingTime);
        Assert.Null(s2.EstimatedRemainingTime);
        Assert.Null(s3.EstimatedRemainingTime);
        Assert.Null(s4.EstimatedRemainingTime);
        Assert.Null(s5.EstimatedRemainingTime);
        Assert.Null(s6.EstimatedRemainingTime);
        Assert.Null(s7.EstimatedRemainingTime);
        Assert.NotNull(snapshot.EstimatedRemainingTime);
        Assert.Equal(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(Math.Round(snapshot.EstimatedRemainingTime!.Value.TotalSeconds)));
        Assert.NotNull(snapshot.SmoothedThroughputBytesPerSecond);
        Assert.InRange(snapshot.SmoothedThroughputBytesPerSecond!.Value, 99, 101);
    }

    [Fact]
    public void UpdateEstimate_WhenPaused_ShouldHideEta()
    {
        var estimator = new BackupEtaEstimator();
        var t0 = new DateTime(2026, 2, 23, 10, 0, 0, DateTimeKind.Utc);

        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 10, 1000, 1000, t0);
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 9, 1000, 900, t0.AddSeconds(2));
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 8, 1000, 800, t0.AddSeconds(4));
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 7, 1000, 700, t0.AddSeconds(6));
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 6, 1000, 600, t0.AddSeconds(8));
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 5, 1000, 500, t0.AddSeconds(10));
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 4, 1000, 400, t0.AddSeconds(12));
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 3, 1000, 300, t0.AddSeconds(14));
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 2, 1000, 200, t0.AddSeconds(16));
        var pausedSnapshot = estimator.UpdateEstimate(1, BackupStatus.Paused, 10, 2, 1000, 200, t0.AddSeconds(18));

        Assert.Null(pausedSnapshot.EstimatedRemainingTime);
        Assert.NotNull(pausedSnapshot.SmoothedThroughputBytesPerSecond);
    }

    [Fact]
    public void Prune_ShouldResetRemovedJobWarmup()
    {
        var estimator = new BackupEtaEstimator();
        var t0 = new DateTime(2026, 2, 23, 10, 0, 0, DateTimeKind.Utc);

        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 10, 1000, 1000, t0);
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 9, 1000, 900, t0.AddSeconds(2));
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 8, 1000, 800, t0.AddSeconds(4));
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 7, 1000, 700, t0.AddSeconds(6));
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 6, 1000, 600, t0.AddSeconds(8));
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 5, 1000, 500, t0.AddSeconds(10));
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 4, 1000, 400, t0.AddSeconds(12));
        estimator.UpdateEstimate(1, BackupStatus.Active, 10, 3, 1000, 300, t0.AddSeconds(14));

        estimator.Prune(Array.Empty<int>());
        var snapshotAfterPrune = estimator.UpdateEstimate(1, BackupStatus.Active, 10, 2, 1000, 200, t0.AddSeconds(16));

        Assert.Null(snapshotAfterPrune.EstimatedRemainingTime);
    }

    [Fact]
    public void UpdateEstimate_ShouldRefineAverageOnEachCompletedFile()
    {
        var estimator = new BackupEtaEstimator();
        var t0 = new DateTime(2026, 2, 23, 10, 0, 0, DateTimeKind.Utc);

        // Warmup done (8 files, >=15s), then one slow file makes average drop.
        estimator.UpdateEstimate(1, BackupStatus.Active, 12, 12, 1200, 1200, t0);
        estimator.UpdateEstimate(1, BackupStatus.Active, 12, 11, 1200, 1100, t0.AddSeconds(2));
        estimator.UpdateEstimate(1, BackupStatus.Active, 12, 10, 1200, 1000, t0.AddSeconds(4));
        estimator.UpdateEstimate(1, BackupStatus.Active, 12, 9, 1200, 900, t0.AddSeconds(6));
        estimator.UpdateEstimate(1, BackupStatus.Active, 12, 8, 1200, 800, t0.AddSeconds(8));
        estimator.UpdateEstimate(1, BackupStatus.Active, 12, 7, 1200, 700, t0.AddSeconds(10));
        estimator.UpdateEstimate(1, BackupStatus.Active, 12, 6, 1200, 600, t0.AddSeconds(12));
        estimator.UpdateEstimate(1, BackupStatus.Active, 12, 5, 1200, 500, t0.AddSeconds(14));
        var beforeSlow = estimator.UpdateEstimate(1, BackupStatus.Active, 12, 4, 1200, 400, t0.AddSeconds(16));
        var afterSlow = estimator.UpdateEstimate(1, BackupStatus.Active, 12, 3, 1200, 300, t0.AddSeconds(20));

        Assert.NotNull(beforeSlow.SmoothedThroughputBytesPerSecond);
        Assert.NotNull(afterSlow.SmoothedThroughputBytesPerSecond);
        Assert.True(afterSlow.SmoothedThroughputBytesPerSecond < beforeSlow.SmoothedThroughputBytesPerSecond);
    }
}
