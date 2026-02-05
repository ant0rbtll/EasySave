using EasySave.Log;

namespace EasySave.Log.Tests;

public class NoOpLoggerTests
{
    [Fact]
    public void Write_DoesNotThrow()
    {
        var logger = new NoOpLogger();
        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc),
            "Job",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            2);

        logger.Write(entry);
    }

    [Fact]
    public void Write_AcceptsAllEventTypes()
    {
        var logger = new NoOpLogger();
        foreach (var eventType in Enum.GetValues<LogEventType>())
        {
            var entry = new LogEntry(
                new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc),
                "Job",
                eventType,
                "src",
                "dst",
                1,
                2);

            logger.Write(entry);
        }
    }

    [Fact]
    public void Write_IsThreadSafeUnderConcurrentCalls()
    {
        var logger = new NoOpLogger();
        var tasks = new List<Task>();

        for (int i = 0; i < 50; i++)
        {
            int copy = i;
            tasks.Add(Task.Run(() =>
            {
                var entry = new LogEntry(
                    new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc),
                    $"Job_{copy}",
                    LogEventType.TransferFile,
                    "src",
                    "dst",
                    copy,
                    copy);
                logger.Write(entry);
            }));
        }

        // Test passes if no exception is thrown during concurrent writes.
        Task.WaitAll(tasks.ToArray());
    }
}
