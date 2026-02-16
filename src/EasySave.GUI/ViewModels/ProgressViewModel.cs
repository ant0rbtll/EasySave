using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Threading;
using EasySave.Application;
using EasySave.GUI.Helpers;
using EasySave.Localization;

namespace EasySave.GUI.ViewModels;

public partial class ProgressViewModel : ViewModelBase
{
    private readonly BackupApplicationService _applicationService;
    private readonly ILocalizationService _localizationService;
    private CancellationTokenSource? _liveRefreshCts;
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

    public bool HasActiveBackups => ActiveBackups.Count > 0;

    public ProgressViewModel(BackupApplicationService applicationService, ILocalizationService localizationService)
    {
        _applicationService = applicationService;
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

        OnPropertyChanged(nameof(TitleText));
        OnPropertyChanged(nameof(SubtitleText));
        OnPropertyChanged(nameof(EmptyTitleText));
        OnPropertyChanged(nameof(EmptySubtitleText));
        OnPropertyChanged(nameof(FilesLabelText));
        OnPropertyChanged(nameof(SizeLabelText));
        OnPropertyChanged(nameof(CurrentSourceLabelText));
        OnPropertyChanged(nameof(CurrentDestinationLabelText));
        OnPropertyChanged(nameof(UpdatedAtLabelText));

        // Rebuild textual formatting when language/culture changes.
        _lastSnapshotSignature = string.Empty;
    }

    public void StartLiveRefresh()
    {
        if (_liveRefreshCts is not null)
            return;

        _liveRefreshCts = new CancellationTokenSource();
        _ = RunLiveRefreshLoopAsync(_liveRefreshCts.Token);
    }

    public void StopLiveRefresh()
    {
        var cts = _liveRefreshCts;
        if (cts is null)
            return;

        _liveRefreshCts = null;
        cts.Cancel();
        cts.Dispose();
    }

    private async Task RunLiveRefreshLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await RefreshOnceAsync(cancellationToken);
            await Task.Delay(LiveRefreshIntervalMs, cancellationToken);
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
            UpdatedAtDisplay = FormatUpdatedAt(state.LastUpdateAt)
        };
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
