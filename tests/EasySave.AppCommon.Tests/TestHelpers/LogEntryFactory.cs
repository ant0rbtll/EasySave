namespace EasySave.AppCommon.Tests.TestHelpers;

internal static class LogEntryFactory
{
    public static LogEntry Create(string name = "TestJob") =>
        new(DateTime.UtcNow, name, LogEventType.TransferFile, "src", "dst", 100, 50);
}
