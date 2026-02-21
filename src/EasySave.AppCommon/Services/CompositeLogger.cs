using EasySave.Core.Exceptions;
using EasySave.Log;

namespace EasySave.AppCommon.Services;

/// <summary>
/// Logger that delegates to multiple inner loggers (e.g. local + centralized).
/// </summary>
public sealed class CompositeLogger(params ILogger[] loggers) : ILogger, IDisposable
{
    private readonly ILogger[] _loggers = loggers ?? throw new InvalidArgumentException(nameof(loggers));

    public void Write(LogEntry entry)
    {
        List<Exception>? exceptions = null;

        foreach (var logger in _loggers)
        {
            try
            {
                logger.Write(entry);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new EasysaveDefaultException("error_logger_failed", [ /*exceptions*/]);
        }
    }

    public void Dispose()
    {
        Exception? firstException = null;

        foreach (var logger in _loggers)
        {
            if (logger is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    firstException ??= ex;
                }
            }
        }

        
        if (firstException != null)
        {
            //TO EXCEPTION
            throw firstException;
        }
    }
}
