using EasySave.Core.Exceptions;
using EasySave.Log;

namespace EasySave.AppCommon.Services;

/// <summary>
/// Wraps <see cref="HttpLogSender"/> with automatic fallback to local file logging
/// when the centralized server is unreachable. Reports server status changes.
/// Performs a health check every 10s to detect server up/down transitions.
/// </summary>
public sealed class ResilientHttpLogSender : ILogger, IDisposable
{
    private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromSeconds(10);

    private readonly HttpLogSender _httpSender;
    private readonly ILogger? _fallbackLogger;
    private readonly LogServerStatusNotifier _statusNotifier;
    private readonly Timer _healthCheckTimer;
    private volatile bool _disposed;

    /// <param name="httpSender">The underlying HTTP log sender.</param>
    /// <param name="statusNotifier">Notifier to report server status changes.</param>
    /// <param name="fallbackLogger">
    /// Fallback local logger. Non-null in Centralized mode (where no other local logger exists).
    /// Null in LocalAndCentralized mode (where CompositeLogger already provides a local logger).
    /// </param>
    public ResilientHttpLogSender(
        HttpLogSender httpSender,
        LogServerStatusNotifier statusNotifier,
        ILogger? fallbackLogger = null)
    {
        _httpSender = httpSender ?? throw new InvalidArgumentException(nameof(httpSender));
        _statusNotifier = statusNotifier ?? throw new InvalidArgumentException(nameof(statusNotifier));
        _fallbackLogger = fallbackLogger;

        // Initial health check (immediate) + periodic every 10s
        _healthCheckTimer = new Timer(OnHealthCheckTick, null, TimeSpan.Zero, HealthCheckInterval);
    }

    public void Write(LogEntry entry)
    {
        EasysaveDefaultException.ThrowIfNull(entry);

        if (_disposed)
            return;

        try
        {
            _httpSender.Write(entry);
            _statusNotifier.ReportStatus(true);
        }
        catch
        {
            _statusNotifier.ReportStatus(false);
            _fallbackLogger?.Write(entry);
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _healthCheckTimer.Dispose();
        _httpSender.Dispose();

        if (_fallbackLogger is IDisposable disposable)
            disposable.Dispose();
    }

    private void OnHealthCheckTick(object? state)
    {
        if (_disposed)
            return;

        try
        {
            _httpSender.CheckServerHealth();
            _statusNotifier.ReportStatus(true);
        }
        catch
        {
            _statusNotifier.ReportStatus(false);
        }
    }
}
