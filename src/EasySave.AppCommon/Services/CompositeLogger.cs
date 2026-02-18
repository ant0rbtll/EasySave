using EasySave.Log;

namespace EasySave.AppCommon.Services;

/// <summary>
/// Logger that delegates to multiple inner loggers (e.g. local + centralized).
/// </summary>
public sealed class CompositeLogger(params ILogger[] loggers) : ILogger, IDisposable
{
    private readonly ILogger[] _loggers = loggers ?? throw new ArgumentNullException(nameof(loggers));

    public void Write(LogEntry entry)
    {
        foreach (var logger in _loggers)
        {
            logger.Write(entry);
        }
    }

    public void Dispose()
    {
        foreach (var logger in _loggers)
        {
            if (logger is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
