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

    [ObservableProperty]
    private bool isStopDialogOpen;

    [ObservableProperty]
    private int? pendingStopJobId;

    [ObservableProperty]
    private string pendingStopJobName = string.Empty;

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
    public string CancelText { get; private set; } = string.Empty;
    public string TooltipPlay { get; private set; } = string.Empty;
    public string TooltipPause { get; private set; } = string.Empty;
    public string TooltipStop { get; private set; } = string.Empty;
    public string StatusRunningText { get; private set; } = string.Empty;
    public string StatusWaitingText { get; private set; } = string.Empty;
    public string StatusPausedText { get; private set; } = string.Empty;
    public string StatusStopRequestedText { get; private set; } = string.Empty;
    public string StatusBlockedBusinessText { get; private set; } = string.Empty;
    public string ConfirmStopTitleText { get; private set; } = string.Empty;
    public string ConfirmStopMessageText { get; private set; } = string.Empty;

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
        CancelText = _localizationService.TranslateText(LocalizationKey.gui_manage_btn_cancel);
        TooltipPlay = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_play);
        TooltipPause = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_pause);
        TooltipStop = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_stop);
        StatusRunningText = _localizationService.TranslateText(LocalizationKey.gui_progress_status_running);
        StatusWaitingText = _localizationService.TranslateText(LocalizationKey.backupjob_waiting);
        StatusPausedText = _localizationService.TranslateText(LocalizationKey.gui_progress_status_paused);
        StatusStopRequestedText = _localizationService.TranslateText(LocalizationKey.gui_progress_status_stopping);
        StatusBlockedBusinessText = _localizationService.TranslateText(LocalizationKey.gui_progress_status_blocked_business);
        ConfirmStopTitleText = _localizationService.TranslateText(LocalizationKey.gui_progress_confirm_stop_title);
        ConfirmStopMessageText = _localizationService.TranslateText(LocalizationKey.gui_progress_confirm_stop_message);

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
        OnPropertyChanged(nameof(CancelText));
        OnPropertyChanged(nameof(TooltipPlay));
        OnPropertyChanged(nameof(TooltipPause));
        OnPropertyChanged(nameof(TooltipStop));
        OnPropertyChanged(nameof(StatusRunningText));
        OnPropertyChanged(nameof(StatusWaitingText));
        OnPropertyChanged(nameof(StatusPausedText));
        OnPropertyChanged(nameof(StatusStopRequestedText));
        OnPropertyChanged(nameof(StatusBlockedBusinessText));
        OnPropertyChanged(nameof(ConfirmStopTitleText));
        OnPropertyChanged(nameof(ConfirmStopMessageText));

        // Rebuild textual formatting when language/culture changes.
        _lastSnapshotSignature = string.Empty;
    }

    [RelayCommand]
    private void PauseJob(int jobId)
    {
        _backupExecutionController.Pause(jobId);
    }

    [RelayCommand]
    private void PlayJob(int jobId)
    {
        _backupExecutionController.Resume(jobId);
    }

    [RelayCommand]
    private void AskStopJob(int jobId)
    {
        PendingStopJobId = jobId;
        PendingStopJobName = ResolveStopTargetName(jobId);
        IsStopDialogOpen = true;
    }

    [RelayCommand]
    private void ConfirmStopJob()
    {
        if (PendingStopJobId is not int jobId)
            return;

        _backupExecutionController.RequestStop(jobId);
        CloseStopDialog();
    }

    [RelayCommand]
    private void CancelStopJob()
    {
        CloseStopDialog();
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

            var orderedStates = states.Values
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.Id)
                .ToList();

            var signature = BuildSignature(orderedStates);
            if (string.Equals(signature, _lastSnapshotSignature, StringComparison.Ordinal))
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SyncActiveBackups(orderedStates);
            });

            _lastSnapshotSignature = signature;
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

    private void SyncActiveBackups(IReadOnlyList<BackupJobLiveProgressState> orderedStates)
    {
        var desiredStateById = orderedStates.ToDictionary(s => s.Id);
        var previousCount = ActiveBackups.Count;

        for (var i = ActiveBackups.Count - 1; i >= 0; i--)
        {
            if (!desiredStateById.ContainsKey(ActiveBackups[i].Id))
            {
                ActiveBackups.RemoveAt(i);
            }
        }

        for (var desiredIndex = 0; desiredIndex < orderedStates.Count; desiredIndex++)
        {
            var state = orderedStates[desiredIndex];
            var existingIndex = FindActiveBackupIndex(state.Id);

            if (existingIndex < 0)
            {
                ActiveBackups.Insert(desiredIndex, CreateItem(state));
                continue;
            }

            if (existingIndex != desiredIndex)
            {
                ActiveBackups.Move(existingIndex, desiredIndex);
            }

            UpdateItemFromState(ActiveBackups[desiredIndex], state);
        }

        if (previousCount != ActiveBackups.Count)
        {
            OnPropertyChanged(nameof(HasActiveBackups));
        }

        if (IsStopDialogOpen
            && PendingStopJobId is int pendingStopId
            && !desiredStateById.ContainsKey(pendingStopId))
        {
            CloseStopDialog();
        }
    }

    private int FindActiveBackupIndex(int jobId)
    {
        for (var i = 0; i < ActiveBackups.Count; i++)
        {
            if (ActiveBackups[i].Id == jobId)
                return i;
        }

        return -1;
    }

    private Models.ActiveBackupProgressItem CreateItem(BackupJobLiveProgressState state)
    {
        var presentation = BuildPresentation(state);

        return new Models.ActiveBackupProgressItem
        {
            Id = state.Id,
            Name = state.Name,
            ProgressPercent = presentation.ProgressPercent,
            ProgressDisplay = presentation.ProgressDisplay,
            FilesDisplay = presentation.FilesDisplay,
            SizeDisplay = presentation.SizeDisplay,
            CurrentSourcePath = presentation.CurrentSourcePath,
            CurrentDestinationPath = presentation.CurrentDestinationPath,
            UpdatedAtDisplay = presentation.UpdatedAtDisplay,
            RuntimeStatusText = presentation.RuntimeStatusText,
            RuntimeStatusBackground = presentation.RuntimeStatusBackground,
            RuntimeStatusForeground = presentation.RuntimeStatusForeground,
            CanPlay = presentation.CanPlay,
            CanPause = presentation.CanPause,
            CanStop = presentation.CanStop
        };
    }

    private void UpdateItemFromState(Models.ActiveBackupProgressItem item, BackupJobLiveProgressState state)
    {
        var presentation = BuildPresentation(state);

        item.Name = state.Name;
        item.ProgressPercent = presentation.ProgressPercent;
        item.ProgressDisplay = presentation.ProgressDisplay;
        item.FilesDisplay = presentation.FilesDisplay;
        item.SizeDisplay = presentation.SizeDisplay;
        item.CurrentSourcePath = presentation.CurrentSourcePath;
        item.CurrentDestinationPath = presentation.CurrentDestinationPath;
        item.UpdatedAtDisplay = presentation.UpdatedAtDisplay;
        item.RuntimeStatusText = presentation.RuntimeStatusText;
        item.RuntimeStatusBackground = presentation.RuntimeStatusBackground;
        item.RuntimeStatusForeground = presentation.RuntimeStatusForeground;
        item.CanPlay = presentation.CanPlay;
        item.CanPause = presentation.CanPause;
        item.CanStop = presentation.CanStop;
    }

    private ActiveBackupPresentation BuildPresentation(BackupJobLiveProgressState state)
    {
        if (state.Status == Core.BackupJobStatus.Blocked)
        {
            return new ActiveBackupPresentation(
                ProgressPercent: state.ProgressPercent,
                ProgressDisplay: $"{state.ProgressPercent}%",
                FilesDisplay: $"{Math.Max(0, state.TotalFiles - state.RemainingFiles)}/{Math.Max(0, state.TotalFiles)}",
                SizeDisplay: $"{LogValueFormatter.FormatFileSize(Math.Max(0, state.TotalSizeBytes - state.RemainingSizeBytes), _localizationService.Culture)}/{LogValueFormatter.FormatFileSize(Math.Max(0, state.TotalSizeBytes), _localizationService.Culture)}",
                CurrentSourcePath: state.CurrentSourcePath ?? "-",
                CurrentDestinationPath: state.CurrentDestinationPath ?? "-",
                UpdatedAtDisplay: FormatUpdatedAt(state.LastUpdateAt),
                RuntimeStatusText: StatusBlockedBusinessText,
                RuntimeStatusBackground: "#2AAF3A3A",
                RuntimeStatusForeground: "#FFC8C8",
                CanPlay: false,
                CanPause: false,
                CanStop: true);
        }

        if (state.Status == Core.BackupJobStatus.Waiting)
        {
            return new ActiveBackupPresentation(
                ProgressPercent: state.ProgressPercent,
                ProgressDisplay: $"{state.ProgressPercent}%",
                FilesDisplay: $"{Math.Max(0, state.TotalFiles - state.RemainingFiles)}/{Math.Max(0, state.TotalFiles)}",
                SizeDisplay: $"{LogValueFormatter.FormatFileSize(Math.Max(0, state.TotalSizeBytes - state.RemainingSizeBytes), _localizationService.Culture)}/{LogValueFormatter.FormatFileSize(Math.Max(0, state.TotalSizeBytes), _localizationService.Culture)}",
                CurrentSourcePath: state.CurrentSourcePath ?? "-",
                CurrentDestinationPath: state.CurrentDestinationPath ?? "-",
                UpdatedAtDisplay: FormatUpdatedAt(state.LastUpdateAt),
                RuntimeStatusText: StatusWaitingText,
                RuntimeStatusBackground: "#2A8B5A2B",
                RuntimeStatusForeground: "#FFD59A",
                CanPlay: false,
                CanPause: true,
                CanStop: true);
        }

        var controlState = ResolveControlState(state.Id);
        var (statusText, statusBackground, statusForeground, canPlay, canPause, canStop) = controlState switch
        {
            BackupJobControlState.Paused => (StatusPausedText, "#2A8B5A2B", "#FFD59A", true, false, true),
            BackupJobControlState.StopRequested => (StatusStopRequestedText, "#2AAF3A3A", "#FFC8C8", false, false, false),
            _ => (StatusRunningText, "#224A90E2", "#CFE8FF", false, true, true)
        };

        return new ActiveBackupPresentation(
            ProgressPercent: state.ProgressPercent,
            ProgressDisplay: $"{state.ProgressPercent}%",
            FilesDisplay: $"{Math.Max(0, state.TotalFiles - state.RemainingFiles)}/{Math.Max(0, state.TotalFiles)}",
            SizeDisplay: $"{LogValueFormatter.FormatFileSize(Math.Max(0, state.TotalSizeBytes - state.RemainingSizeBytes), _localizationService.Culture)}/{LogValueFormatter.FormatFileSize(Math.Max(0, state.TotalSizeBytes), _localizationService.Culture)}",
            CurrentSourcePath: state.CurrentSourcePath ?? "-",
            CurrentDestinationPath: state.CurrentDestinationPath ?? "-",
            UpdatedAtDisplay: FormatUpdatedAt(state.LastUpdateAt),
            RuntimeStatusText: statusText,
            RuntimeStatusBackground: statusBackground,
            RuntimeStatusForeground: statusForeground,
            CanPlay: canPlay,
            CanPause: canPause,
            CanStop: canStop);
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

    private string BuildSignature(IReadOnlyList<BackupJobLiveProgressState> orderedStates)
    {
        if (orderedStates.Count == 0)
            return "empty";

        return string.Join("|",
            orderedStates.Select(s =>
            {
                var controlState = ResolveControlState(s.Id);
                return $"{s.Id}:{s.Name}:{s.Status}:{s.ProgressPercent}:{s.TotalFiles}:{s.TotalSizeBytes}:{s.RemainingFiles}:{s.RemainingSizeBytes}:{s.CurrentSourcePath}:{s.CurrentDestinationPath}:{controlState}";
            }));
    }

    private void CloseStopDialog()
    {
        IsStopDialogOpen = false;
        PendingStopJobId = null;
        PendingStopJobName = string.Empty;
    }

    private string ResolveStopTargetName(int jobId)
    {
        var item = ActiveBackups.FirstOrDefault(active => active.Id == jobId);
        if (item is not null && !string.IsNullOrWhiteSpace(item.Name))
            return item.Name;

        try
        {
            var liveStates = _applicationService.GetAllJobsLiveProgress();
            if (liveStates.TryGetValue(jobId, out var state) && !string.IsNullOrWhiteSpace(state.Name))
                return state.Name;
        }
        catch (Exception)
        {
            // Ignore transient state read errors and fallback to id.
        }

        return $"#{jobId.ToString(CultureInfo.InvariantCulture)}";
    }

    private readonly record struct ActiveBackupPresentation(
        int ProgressPercent,
        string ProgressDisplay,
        string FilesDisplay,
        string SizeDisplay,
        string CurrentSourcePath,
        string CurrentDestinationPath,
        string UpdatedAtDisplay,
        string RuntimeStatusText,
        string RuntimeStatusBackground,
        string RuntimeStatusForeground,
        bool CanPlay,
        bool CanPause,
        bool CanStop);
}
