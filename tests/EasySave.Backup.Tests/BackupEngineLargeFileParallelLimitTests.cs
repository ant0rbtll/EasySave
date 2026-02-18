using EasySave.Core;
using EasySave.Log;
using EasySave.State;
using EasySave.System;
using Moq;

namespace EasySave.Backup.Tests;

public class BackupEngineLargeFileParallelLimitTests
{
    [Fact]
    public async Task Execute_TwoLargeFiles_DoNotTransferInParallel()
    {
        var fileSystem = new Mock<IFileSystem>();
        var transferService = new TrackingTransferService();
        var stateWriter = new Mock<IStateWriter>();
        var logger = new Mock<ILogger>();

        var barrier = new InMemoryLargeFileTransferBarrier();
        var engine1 = CreateEngine(fileSystem.Object, transferService, stateWriter.Object, logger.Object, barrier);
        var engine2 = CreateEngine(fileSystem.Object, transferService, stateWriter.Object, logger.Object, barrier);

        const string source1 = "/source/job1";
        const string source2 = "/source/job2";
        const string file1 = "/source/job1/large1.bin";
        const string file2 = "/source/job2/large2.bin";
        const string dest1 = "/destination/job1/large1.bin";
        const string dest2 = "/destination/job2/large2.bin";

        SetupSingleFilePlan(fileSystem, source1, file1, dest1, 2 * 1024);
        SetupSingleFilePlan(fileSystem, source2, file2, dest2, 2 * 1024);

        var job1 = new BackupJob { Id = 1, Name = "Job1", Source = source1, Destination = "/destination/job1", Type = BackupType.Complete };
        var job2 = new BackupJob { Id = 2, Name = "Job2", Source = source2, Destination = "/destination/job2", Type = BackupType.Complete };
        var executionContext = new BackupExecutionContext(parallelLargeFileThresholdBytes: 1024);

        await Task.WhenAll(
            engine1.Execute(job1, executionContext: executionContext),
            engine2.Execute(job2, executionContext: executionContext));

        Assert.Equal(1, transferService.MaxConcurrentLargeTransfers);
        stateWriter.Verify(
            sw => sw.Update(It.Is<StateEntry>(entry => entry.Status == BackupStatus.Waiting)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Execute_LargeAndSmallFiles_SmallFileCanTransferWhileLargeIsRunning()
    {
        var fileSystem = new Mock<IFileSystem>();
        var transferService = new TrackingTransferService();
        var stateWriter = new Mock<IStateWriter>();
        var logger = new Mock<ILogger>();

        var barrier = new InMemoryLargeFileTransferBarrier();
        var engine1 = CreateEngine(fileSystem.Object, transferService, stateWriter.Object, logger.Object, barrier);
        var engine2 = CreateEngine(fileSystem.Object, transferService, stateWriter.Object, logger.Object, barrier);

        const string sourceLarge = "/source/large";
        const string sourceSmall = "/source/small";
        const string largeFile = "/source/large/large.bin";
        const string smallFile = "/source/small/small.bin";
        const string largeDest = "/destination/large/large.bin";
        const string smallDest = "/destination/small/small.bin";

        SetupSingleFilePlan(fileSystem, sourceLarge, largeFile, largeDest, 2 * 1024);
        SetupSingleFilePlan(fileSystem, sourceSmall, smallFile, smallDest, 128);

        var largeJob = new BackupJob { Id = 3, Name = "Large", Source = sourceLarge, Destination = "/destination/large", Type = BackupType.Complete };
        var smallJob = new BackupJob { Id = 4, Name = "Small", Source = sourceSmall, Destination = "/destination/small", Type = BackupType.Complete };
        var executionContext = new BackupExecutionContext(parallelLargeFileThresholdBytes: 1024);

        await Task.WhenAll(
            engine1.Execute(largeJob, executionContext: executionContext),
            engine2.Execute(smallJob, executionContext: executionContext));

        Assert.True(transferService.SmallTransferStartedBeforeLargeCompleted);
    }

    private static BackupEngine CreateEngine(
        IFileSystem fileSystem,
        ITransferService transferService,
        IStateWriter stateWriter,
        ILogger logger,
        ILargeFileTransferBarrier barrier)
    {
        return new BackupEngine(
            fileSystem,
            transferService,
            stateWriter,
            logger,
            executionGuard: Mock.Of<IBackupExecutionGuard>(),
            largeFileTransferBarrier: barrier);
    }

    private static void SetupSingleFilePlan(
        Mock<IFileSystem> fileSystem,
        string sourceRoot,
        string sourceFile,
        string destinationFile,
        long fileSizeBytes)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationFile)!;

        fileSystem
            .Setup(fs => fs.EnumerateFilesRecursive(sourceRoot, It.IsAny<IEnumerable<string>>()))
            .Returns([sourceFile]);
        fileSystem
            .Setup(fs => fs.GetFileSize(sourceFile))
            .Returns(fileSizeBytes);
        fileSystem
            .Setup(fs => fs.DirectoryExists(destinationDirectory))
            .Returns(true);
    }

    private sealed class TrackingTransferService : ITransferService
    {
        private int _activeLargeTransfers;
        private int _maxConcurrentLargeTransfers;
        private long _largeStartTicks;
        private long _largeEndTicks;
        private long _smallStartTicks;

        public int MaxConcurrentLargeTransfers => _maxConcurrentLargeTransfers;

        public bool SmallTransferStartedBeforeLargeCompleted
            => _smallStartTicks > 0
               && _largeEndTicks > 0
               && _smallStartTicks < _largeEndTicks;

        public TransferResult TransferFile(string sourcePath, string destinationPath, bool overwrite)
        {
            var isLarge = sourcePath.Contains("large", StringComparison.OrdinalIgnoreCase);

            if (isLarge)
            {
                Interlocked.Exchange(ref _largeStartTicks, DateTime.UtcNow.Ticks);
                var concurrent = Interlocked.Increment(ref _activeLargeTransfers);
                UpdateMaxConcurrent(concurrent);
                Thread.Sleep(250);
                Interlocked.Decrement(ref _activeLargeTransfers);
                Interlocked.Exchange(ref _largeEndTicks, DateTime.UtcNow.Ticks);
                return new TransferResult(2048, 250, 0);
            }

            Interlocked.Exchange(ref _smallStartTicks, DateTime.UtcNow.Ticks);
            Thread.Sleep(50);
            return new TransferResult(128, 50, 0);
        }

        private void UpdateMaxConcurrent(int current)
        {
            while (true)
            {
                var previous = _maxConcurrentLargeTransfers;
                if (current <= previous)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _maxConcurrentLargeTransfers, current, previous) == previous)
                {
                    return;
                }
            }
        }
    }
}
