using EasySave.Configuration;
using EasySave.Core;
using EasySave.Log;
using EasySave.Persistence;

namespace EasySave.GUI.Services;

/// <summary>
/// Runtime-reloadable logger wrapper used by the GUI.
/// </summary>
public sealed class ReloadableLogger : ILogger, ILoggerRuntimeReloader, IDisposable
{
    private const string EasyLogDailyFileMutexName = "Global\\ProSoft_EasySave_EasyLog_DailyFile";

    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IPathProvider _pathProvider;
    private readonly object _sync = new();

    private ILogger _currentLogger;

    public ReloadableLogger(IUserPreferencesRepository preferencesRepository, IPathProvider pathProvider)
    {
        _preferencesRepository = preferencesRepository;
        _pathProvider = pathProvider;
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

            EasyLog.ILogFormatter formatter = userPreferences.LogFormat == LogFormat.Xml
                ? new EasyLog.XmlLogFormatter()
                : new EasyLog.JsonLogFormatter();

            return new EasyLog.DailyFileLogger(
                formatter,
                _pathProvider,
                EasyLogDailyFileMutexName,
                userPreferences.LogFormat);
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
}
