using EasySave.Configuration;
using EasySave.Core;
using EasySave.Log;
using EasySave.Persistence;

namespace EasySave.AppCommon.Services;

/// <summary>
/// Runtime-reloadable logger wrapper used by application hosts.
/// </summary>
public sealed class ReloadableLogger : ILogger, ILoggerRuntimeReloader, IDisposable
{
    private const string EasyLogDailyFileMutexName = "Global\\ProSoft_EasySave_EasyLog_DailyFile";

    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IPathProvider _pathProvider;
    private readonly LogServerStatusNotifier _statusNotifier;
    private readonly object _sync = new();

    private ILogger _currentLogger;

    public ReloadableLogger(
        IUserPreferencesRepository preferencesRepository,
        IPathProvider pathProvider,
        LogServerStatusNotifier statusNotifier)
    {
        _preferencesRepository = preferencesRepository;
        _pathProvider = pathProvider;
        _statusNotifier = statusNotifier;
        _currentLogger = CreateConfiguredLogger();
    }

    public void Write(LogEntry entry)
    {
        lock (_sync)
        {
            _currentLogger.Write(entry);
        }
    }

    public void Reload()
    {
        var nextLogger = CreateConfiguredLogger();

        lock (_sync)
        {
            var previous = _currentLogger;
            _currentLogger = nextLogger;

            if (previous is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_currentLogger is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private ILogger CreateConfiguredLogger()
    {
        try
        {
            var userPreferences = _preferencesRepository.Load();
            _pathProvider.SetLogDirectoryOverride(userPreferences.LogDirectory);

            var logMode = userPreferences.LogMode;

            ILogger? localLogger = null;
            if (logMode == LogMode.Local || logMode == LogMode.LocalAndCentralized)
            {
                localLogger = CreateLocalLogger(userPreferences.LogFormat);
            }

            ILogger? remoteLogger = null;
            if ((logMode == LogMode.Centralized || logMode == LogMode.LocalAndCentralized)
                && !string.IsNullOrWhiteSpace(userPreferences.LogServerUrl))
            {
                var httpSender = new HttpLogSender(userPreferences.LogServerUrl, userPreferences.LogFormat);

                if (logMode == LogMode.Centralized)
                {
                    var fallbackLogger = CreateLocalLogger(userPreferences.LogFormat);
                    remoteLogger = new ResilientHttpLogSender(httpSender, _statusNotifier, fallbackLogger);
                }
                else
                {
                    remoteLogger = new ResilientHttpLogSender(httpSender, _statusNotifier);
                }
            }
            else
            {
                _statusNotifier.ReportStatus(true);
            }

            return logMode switch
            {
                LogMode.Centralized => remoteLogger ?? new NoOpLogger(),
                LogMode.LocalAndCentralized => (localLogger, remoteLogger) switch
                {
                    (not null, not null) => new CompositeLogger(localLogger, remoteLogger),
                    (not null, null) => localLogger,
                    (null, not null) => remoteLogger,
                    _ => new NoOpLogger()
                },
                _ => localLogger ?? new NoOpLogger()
            };
        }
        catch (Exception ex)
        {
            try
            {
                Console.Error.WriteLine($"EasyLog reload failed: {ex}");
            }
            catch
            {
                // Best-effort logging only.
            }

            return new NoOpLogger();
        }
    }

    private ILogger CreateLocalLogger(LogFormat logFormat)
    {
        EasyLog.ILogFormatter formatter = logFormat == LogFormat.Xml
            ? new EasyLog.XmlLogFormatter()
            : new EasyLog.JsonLogFormatter();

        return new EasyLog.DailyFileLogger(
            formatter,
            _pathProvider,
            EasyLogDailyFileMutexName,
            logFormat);
    }
}
