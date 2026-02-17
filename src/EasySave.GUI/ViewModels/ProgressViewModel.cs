using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Application;
using EasySave.Backup;
using EasySave.GUI.Helpers;
using EasySave.Localization;

namespace EasySave.GUI.ViewModels;

public partial class ProgressViewModel : ViewModelBase
{
    private readonly BackupApplicationService _applicationService;
    private readonly IBackupExecutionController _backupExecutionController;
    private readonly ILocalizationService _localizationService;
    private CancellationTokenSource? _liveRefreshCts;
    private Task? _liveRefreshTask;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private const int LiveRefreshIntervalMs = 500;
    private string _lastSnapshotSignature = string.Empty;

    public ObservableCollection<Models.ActiveBackupProgressItem> ActiveBackups { get; } = [];

    public string TitleText { get; private set; } = string.Empty;
    public string SubtitleText { get; private set; } = string.Empty;
    public string EmptyTitleText { get; private set; } = string.Empty;
    public string EmptySubtitleText { get; private set; } = string.Empty;
    public string FilesLabelText { get; private set; } = string.Empty;
    public string SizeLabelText { get; private set; } = string.Empty;
    public string CurrentSourceLabelText { get; private set; } = string.Empty;
    public string CurrentDestinationLabelText { get; private set; } = string.Empty;
    public string UpdatedAtLabelText { get; private set; } = string.Empty;
    public string RuntimeActionsLabelText { get; private set; } = string.Empty;
    public string PlayText { get; private set; } = string.Empty;
    public string PauseText { get; private set; } = string.Empty;
    public string StopText { get; private set; } = string.Empty;
    public string TooltipPlay { get; private set; } = string.Empty;
    public string TooltipPause { get; private set; } = string.Empty;
    public string TooltipStop { get; private set; } = string.Empty;
    public string StatusRunningText { get; private set; } = string.Empty;
    public string StatusPausedText { get; private set; } = string.Empty;
    public string StatusStopRequestedText { get; private set; } = string.Empty;
    public string StatusBlockedBusinessText { get; private set; } = string.Empty;

    public bool HasActiveBackups => ActiveBackups.Count > 0;

    public ProgressViewModel(
        BackupApplicationService applicationService,
        IBackupExecutionController backupExecutionController,
        ILocalizationService localizationService)
    {
        _applicationService = applicationService;
        _backupExecutionController = backupExecutionController;
        _localizationService = localizationService;
        RefreshTranslations();
    }

    public void RefreshTranslations()
    {
        TitleText = _localizationService.TranslateText(LocalizationKey.gui_progress_title);
        SubtitleText = _localizationService.TranslateText(LocalizationKey.gui_progress_subtitle);
        EmptyTitleText = _localizationService.TranslateText(LocalizationKey.gui_progress_empty_title);
        EmptySubtitleText = _localizationService.TranslateText(LocalizationKey.gui_progress_empty_subtitle);
        FilesLabelText = _localizationService.TranslateText(LocalizationKey.gui_progress_label_files);
        SizeLabelText = _localizationService.TranslateText(LocalizationKey.gui_progress_label_size);
        CurrentSourceLabelText = _localizationService.TranslateText(LocalizationKey.gui_progress_label_current_source);
        CurrentDestinationLabelText = _localizationService.TranslateText(LocalizationKey.gui_progress_label_current_destination);
        UpdatedAtLabelText = _localizationService.TranslateText(LocalizationKey.gui_progress_label_updated_at);
        RuntimeActionsLabelText = _localizationService.TranslateText(LocalizationKey.gui_manage_running);
        PlayText = _localizationService.TranslateText(LocalizationKey.gui_manage_play);
        PauseText = _localizationService.TranslateText(LocalizationKey.gui_manage_pause);
        StopText = _localizationService.TranslateText(LocalizationKey.gui_manage_stop);
        TooltipPlay = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_play);
        TooltipPause = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_pause);
        TooltipStop = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_stop);
        StatusRunningText = _localizationService.TranslateText(LocalizationKey.gui_progress_status_running);
        StatusPausedText = _localizationService.TranslateText(LocalizationKey.gui_progress_status_paused);
        StatusStopRequestedText = _localizationService.TranslateText(LocalizationKey.gui_progress_status_stopping);
        StatusBlockedBusinessText = _localizationService.TranslateText(LocalizationKey.gui_progress_status_blocked_business);

        OnPropertyChanged(nameof(TitleText));
        OnPropertyChanged(nameof(SubtitleText));
        OnPropertyChanged(nameof(EmptyTitleText));
        OnPropertyChanged(nameof(EmptySubtitleText));
        OnPropertyChanged(nameof(FilesLabelText));
        OnPropertyChanged(nameof(SizeLabelText));
        OnPropertyChanged(nameof(CurrentSourceLabelText));
        OnPropertyChanged(nameof(CurrentDestinationLabelText));
        OnPropertyChanged(nameof(UpdatedAtLabelText));
        OnPropertyChanged(nameof(RuntimeActionsLabelText));
        OnPropertyChanged(nameof(PlayText));
        OnPropertyChanged(nameof(PauseText));
        OnPropertyChanged(nameof(StopText));
        OnPropertyChanged(nameof(TooltipPlay));
        OnPropertyChanged(nameof(TooltipPause));
        OnPropertyChanged(nameof(TooltipStop));
        OnPropertyChanged(nameof(StatusRunningText));
        OnPropertyChanged(nameof(StatusPausedText));
        OnPropertyChanged(nameof(StatusStopRequestedText));
        OnPropertyChanged(nameof(StatusBlockedBusinessText));

        // Rebuild textual formatting when language/culture changes.
        _lastSnapshotSignature = string.Empty;
    }

    [RelayCommand]
    private void PauseJob(int jobId)
    {
        _backupExecutionController.PauseForJob(jobId);
    }

    [RelayCommand]
    private void PlayJob(int jobId)
    {
        _backupExecutionController.ResumeForJob(jobId);
    }

    [RelayCommand]
    private void StopJob(int jobId)
    {
        _backupExecutionController.RequestStopForJob(jobId);
    }

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
                await RefreshOnceAsync(cancellationToken);
                await Task.Delay(LiveRefreshIntervalMs, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when polling is stopped.
        }
    }

    private async Task RefreshOnceAsync(CancellationToken cancellationToken)
    {
        if (!await _refreshGate.WaitAsync(0, cancellationToken))
            return;

        try
        {
            var states = await Task.Run(
                () => _applicationService.GetAllJobsLiveProgress(),
                cancellationToken);

            var signature = BuildSignature(states);
            if (string.Equals(signature, _lastSnapshotSignature, StringComparison.Ordinal))
                return;

            _lastSnapshotSignature = signature;
            var items = states.Values
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.Id)
                .Select(MapItem)
                .ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ActiveBackups.Clear();
                foreach (var item in items)
                    ActiveBackups.Add(item);

                OnPropertyChanged(nameof(HasActiveBackups));
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
            _refreshGate.Release();
        }
    }

    private Models.ActiveBackupProgressItem MapItem(BackupJobLiveProgressState state)
    {
        if (string.Equals(state.CurrentSourcePath, "error_business_software_running", StringComparison.Ordinal))
        {
            return new Models.ActiveBackupProgressItem
            {
                Id = state.Id,
                Name = state.Name,
                ProgressPercent = state.ProgressPercent,
                ProgressDisplay = $"{state.ProgressPercent}%",
                FilesDisplay = $"{Math.Max(0, state.TotalFiles - state.RemainingFiles)}/{Math.Max(0, state.TotalFiles)}",
                SizeDisplay = $"{LogValueFormatter.FormatFileSize(Math.Max(0, state.TotalSizeBytes - state.RemainingSizeBytes))}/{LogValueFormatter.FormatFileSize(Math.Max(0, state.TotalSizeBytes))}",
                CurrentSourcePath = state.CurrentSourcePath ?? "-",
                CurrentDestinationPath = state.CurrentDestinationPath ?? "-",
                UpdatedAtDisplay = FormatUpdatedAt(state.LastUpdateAt),
                RuntimeStatusText = StatusBlockedBusinessText,
                RuntimeStatusBackground = "#2AAF3A3A",
                RuntimeStatusForeground = "#FFC8C8",
                CanPlay = false,
                CanPause = false,
                CanStop = false
            };
        }

        var controlState = ResolveControlState(state.Id);
        var (statusText, statusBackground, statusForeground, canPlay, canPause, canStop) = controlState switch
        {
            BackupJobControlState.Paused => (StatusPausedText, "#2A8B5A2B", "#FFD59A", true, false, true),
            BackupJobControlState.StopRequested => (StatusStopRequestedText, "#2AAF3A3A", "#FFC8C8", false, false, false),
            _ => (StatusRunningText, "#224A90E2", "#CFE8FF", false, true, true)
        };

        return new Models.ActiveBackupProgressItem
        {
            Id = state.Id,
            Name = state.Name,
            ProgressPercent = state.ProgressPercent,
            ProgressDisplay = $"{state.ProgressPercent}%",
            FilesDisplay = $"{Math.Max(0, state.TotalFiles - state.RemainingFiles)}/{Math.Max(0, state.TotalFiles)}",
            SizeDisplay = $"{LogValueFormatter.FormatFileSize(Math.Max(0, state.TotalSizeBytes - state.RemainingSizeBytes))}/{LogValueFormatter.FormatFileSize(Math.Max(0, state.TotalSizeBytes))}",
            CurrentSourcePath = state.CurrentSourcePath ?? "-",
            CurrentDestinationPath = state.CurrentDestinationPath ?? "-",
            UpdatedAtDisplay = FormatUpdatedAt(state.LastUpdateAt),
            RuntimeStatusText = statusText,
            RuntimeStatusBackground = statusBackground,
            RuntimeStatusForeground = statusForeground,
            CanPlay = canPlay,
            CanPause = canPause,
            CanStop = canStop
        };
    }

    private BackupJobControlState ResolveControlState(int jobId)
    {
        if (_backupExecutionController.TryGetCurrentJobControlState(jobId, out var controlState))
            return controlState;

        return BackupJobControlState.Running;
    }

    private string FormatUpdatedAt(DateTime? updatedAt)
    {
        if (!updatedAt.HasValue)
            return "-";

        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(_localizationService.Culture);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.CurrentCulture;
        }

        return updatedAt.Value.ToString("T", culture);
    }

    private static string BuildSignature(IReadOnlyDictionary<int, BackupJobLiveProgressState> states)
    {
        if (states.Count == 0)
            return "empty";

        return string.Join("|",
            states.OrderBy(kvp => kvp.Key).Select(kvp =>
            {
                var s = kvp.Value;
                return $"{s.Id}:{s.ProgressPercent}:{s.RemainingFiles}:{s.RemainingSizeBytes}:{s.CurrentSourcePath}:{s.CurrentDestinationPath}:{s.LastUpdateAt:O}";
            }));
    }
}
