using EasySave.System;

namespace EasySave.Application.Tests;

public class InMemoryPriorityFilesBarrierTests
{
    [Fact]
    public async Task WaitUntilNoPriorityPendingAsync_WhenNoPriorityPending_CompletesImmediately()
    {
        IPriorityFilesBarrier barrier = new InMemoryPriorityFilesBarrier();

        await barrier.WaitUntilNoPriorityPendingAsync();
    }

    [Fact]
    public async Task WaitUntilNoPriorityPendingAsync_WaitsUntilAllPriorityFilesAreCompletedAcrossJobs()
    {
        IPriorityFilesBarrier barrier = new InMemoryPriorityFilesBarrier();
        barrier.RegisterJob(1, 2);
        barrier.RegisterJob(2, 1);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var waitTask = barrier.WaitUntilNoPriorityPendingAsync(cts.Token);

        Assert.False(waitTask.IsCompleted);

        barrier.MarkPriorityFileCompleted(1);
        Assert.False(waitTask.IsCompleted);

        barrier.MarkPriorityFileCompleted(2);
        Assert.False(waitTask.IsCompleted);

        barrier.MarkPriorityFileCompleted(1);
        await waitTask;
    }

    [Fact]
    public async Task UnregisterJob_ReleasesRemainingPriorityFiles()
    {
        IPriorityFilesBarrier barrier = new InMemoryPriorityFilesBarrier();
        barrier.RegisterJob(42, 3);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var waitTask = barrier.WaitUntilNoPriorityPendingAsync(cts.Token);

        Assert.False(waitTask.IsCompleted);

        barrier.UnregisterJob(42);
        await waitTask;
    }
}
