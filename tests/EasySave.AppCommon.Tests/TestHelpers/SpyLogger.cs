namespace EasySave.AppCommon.Tests.TestHelpers;

internal sealed class SpyLogger : ILogger, IDisposable
{
    public List<LogEntry> WrittenEntries { get; } = new();
    public bool Disposed { get; private set; }

    public void Write(LogEntry entry)
    {
        WrittenEntries.Add(entry);
    }

    public void Dispose() => Disposed = true;
}
