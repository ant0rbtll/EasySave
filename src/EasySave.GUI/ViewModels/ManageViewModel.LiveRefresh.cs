using Avalonia.Threading;
using EasySave.Application;
using EasySave.GUI.Services;
using System.Globalization;

namespace EasySave.GUI.ViewModels;

public partial class ManageViewModel
{
    private CancellationTokenSource? _dismissCts;
    private CancellationTokenSource? _liveRefreshCts;
    private Task? _liveRefreshTask;
    private readonly SemaphoreSlim _runtimeRefreshGate = new(1, 1);
    private const int LiveRefreshIntervalMs = 500;

    public void StartLiveRefresh()
    {
        if (_liveRefreshCts is not null)
            return;

        _liveRefreshCts = new CancellationTokenSource();
        _liveRefreshTask = RunLiveRefreshLoopAsync(_liveRefreshCts.Token);
    }

    public void StopLiveRefresh()
    {
        var cts = _liveRefreshCts;
        if (cts is null)
            return;

        _liveRefreshCts = null;
        cts.Cancel();
        cts.Dispose();
        _liveRefreshTask = null;
    }

    private async Task RunLiveRefreshLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await RefreshRuntimeStatesOnceAsync(cancellationToken);
                await Task.Delay(LiveRefreshIntervalMs, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when polling is stopped.
        }
    }

    private async Task RefreshRuntimeStatesOnceAsync(CancellationToken cancellationToken)
    {
        if (!await _runtimeRefreshGate.WaitAsync(0, cancellationToken))
            return;

        try
        {
            var runtimeStates = await Task.Run(
                () => _applicationService.GetAllJobsRuntimeStates(),
                cancellationToken);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ApplyRuntimeStates(runtimeStates);
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Polling stopped.
        }
        catch (Exception)
        {
            // Ignore transient state read issues during live refresh.
        }
        finally
        {
            _runtimeRefreshGate.Release();
        }
    }

    private void ApplyRuntimeStates(IReadOnlyDictionary<int, BackupJobRuntimeState> runtimeStates)
    {
        if (_allJobs.Count == 0)
            return;

        if (runtimeStates.Count != _allJobs.Count || _allJobs.Any(j => !runtimeStates.ContainsKey(j.Id)))
        {
            SyncRunningJobs(runtimeStates);
            ApplyJobs(_displayService.FetchJobs());
            return;
        }

        var runtimeChanged = false;
        foreach (var job in _allJobs)
        {
            var state = runtimeStates[job.Id];
            var statusDisplay = _displayService.FormatStatus(state.Status);
            var lastExecutionDisplay = _displayService.FormatLastExecution(state.LastExecutionDate);

            if (job.Status != state.Status)
            {
                job.Status = state.Status;
                runtimeChanged = true;
            }

            if (job.IsActive != state.IsActive)
            {
                job.IsActive = state.IsActive;
                runtimeChanged = true;
            }

            if (job.LastExecutionDate != state.LastExecutionDate)
            {
                job.LastExecutionDate = state.LastExecutionDate;
                runtimeChanged = true;
            }

            if (!string.Equals(job.StatusDisplay, statusDisplay, StringComparison.Ordinal))
            {
                job.StatusDisplay = statusDisplay;
                runtimeChanged = true;
            }

            if (!string.Equals(job.LastExecutionDisplay, lastExecutionDisplay, StringComparison.Ordinal))
            {
                job.LastExecutionDisplay = lastExecutionDisplay;
                runtimeChanged = true;
            }
        }

        runtimeChanged |= SyncRunningJobs(runtimeStates);

        if (!runtimeChanged)
        {
            RefreshPauseState();
            return;
        }

        if (!string.IsNullOrWhiteSpace(SearchText) || SortColumn is "Status" or "LastRun")
        {
            Refresh();
        }

        RefreshPauseState();
    }

    private void RefreshRunningState(string? runningLabel = null)
    {
        IsRunning = _stateTracker.HasRunningJobs;

        if (!IsRunning)
        {
            RunningJobName = string.Empty;
            IsPaused = false;
            return;
        }

        if (!string.IsNullOrWhiteSpace(runningLabel))
        {
            RunningJobName = runningLabel;
            RefreshPauseState();
            return;
        }

        RunningJobName = _stateTracker.RunningCount == 1
            ? _stateTracker.ResolveRunningJobLabel(_stateTracker.RunningJobIds.First(), _allJobs)
            : _stateTracker.RunningCount.ToString(CultureInfo.InvariantCulture);

        RefreshPauseState();
    }

    private void RefreshPauseState()
    {
        IsPaused = _stateTracker.ComputeIsPaused(PendingJob?.Id);
        PauseRunningCommand.NotifyCanExecuteChanged();
        PlayRunningCommand.NotifyCanExecuteChanged();
        StopRunningCommand.NotifyCanExecuteChanged();
    }

    private bool SyncRunningJobs(IReadOnlyDictionary<int, BackupJobRuntimeState> runtimeStates)
    {
        var changed = _stateTracker.SyncFromRuntimeStates(runtimeStates);
        if (changed)
        {
            RefreshRunningState();
            RunJobCommand.NotifyCanExecuteChanged();
            RunSelectedCommand.NotifyCanExecuteChanged();
        }
        return changed;
    }
}
