using EasySave.Log;

namespace EasyLog.Tests;

public class LogEntryTests
{
    [Fact]
    public void Ctor_SetsBaseProperties()
    {
        var timestamp = new DateTime(2026, 2, 5, 10, 11, 12, DateTimeKind.Utc);
        var entry = new EasyLog.LogEntry(
            timestamp,
            "JobA",
            LogEventType.TransferFile,
            "src",
            "dst",
            123,
            456);

        var asBase = (EasySave.Log.LogEntry)entry;

        Assert.Equal(timestamp, asBase.Timestamp);
        Assert.Equal("JobA", asBase.BackupName);
        Assert.Equal(LogEventType.TransferFile, asBase.EventType);
        Assert.Equal("src", asBase.SourcePathUNC);
        Assert.Equal("dst", asBase.DestinationPathUNC);
        Assert.Equal(123, asBase.FileSizeBytes);
        Assert.Equal(456, asBase.TransferTimeMs);
    }

    [Fact]
    public void With_ProducesUpdatedEntry()
    {
        var original = new EasyLog.LogEntry(
            new DateTime(2026, 2, 5, 8, 0, 0, DateTimeKind.Utc),
            "JobA",
            LogEventType.CreateDirectory,
            "src",
            "dst",
            1,
            2);

        var updated = original with { BackupName = "JobB", FileSizeBytes = 99 };

        Assert.Equal("JobA", original.BackupName);
        Assert.Equal("JobB", updated.BackupName);
        Assert.Equal(99, updated.FileSizeBytes);
        Assert.Equal(original.Timestamp, updated.Timestamp);
    }
}
