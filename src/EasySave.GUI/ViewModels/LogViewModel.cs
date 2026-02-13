using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Application;
using EasySave.GUI.Models;
using EasySave.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace EasySave.GUI.ViewModels;

/// <summary>
/// ViewModel for the Log view, handling date selection, backup job grouping, run selection and detailed log display.
/// </summary>
public partial class LogViewModel : ViewModelBase
{
    private readonly ILogQueryService _logQueryService;
    private readonly ILogNavigationService _logNavigationService;
    private readonly ILocalizationService _localizationService;
    private readonly List<string> _allAvailableDates = new();
    private readonly List<LogJobSummaryModel> _allBackupJobs = new();
    private readonly List<LogRunSummaryModel> _allRuns = new();
    private readonly List<LogDisplayModel> _allLogEntries = new();
    private Action _onLanguageChanged = static () => { };
    private bool _hasAppliedDefaultDateSelection;

    public ObservableCollection<string> AvailableDates { get; } = new();
    public ObservableCollection<LogJobSummaryModel> BackupJobs { get; } = new();
    public ObservableCollection<LogRunSummaryModel> Runs { get; } = new();
    public ObservableCollection<LogDisplayModel> LogEntries { get; } = new();
    public ObservableCollection<PaginationItem> PaginationItems { get; } = new();

    public IReadOnlyList<int> PageSizeOptions { get; } = [15, 25, 50];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _searchTextDate = string.Empty;
    [ObservableProperty] private string? _selectedDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsJobListVisible))]
    [NotifyPropertyChangedFor(nameof(IsRunListVisible))]
    [NotifyPropertyChangedFor(nameof(IsRunEntriesVisible))]
    [NotifyPropertyChangedFor(nameof(IsPaginationVisible))]
    private bool _isDateSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsJobListVisible))]
    [NotifyPropertyChangedFor(nameof(IsRunListVisible))]
    [NotifyPropertyChangedFor(nameof(IsRunEntriesVisible))]
    [NotifyPropertyChangedFor(nameof(IsPaginationVisible))]
    private bool _isBackupJobSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsJobListVisible))]
    [NotifyPropertyChangedFor(nameof(IsRunListVisible))]
    [NotifyPropertyChangedFor(nameof(IsRunEntriesVisible))]
    [NotifyPropertyChangedFor(nameof(IsPaginationVisible))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private bool _isRunSelected;

    [ObservableProperty] private int _selectedBackupId;
    [ObservableProperty] private string _selectedBackupName = string.Empty;
    [ObservableProperty] private string _selectedRunTitle = string.Empty;
    [ObservableProperty] private string _pageInputText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int _currentPage = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyPropertyChangedFor(nameof(IsPaginationVisible))]
    [NotifyPropertyChangedFor(nameof(PageJumpWatermark))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int _filteredLogsCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyPropertyChangedFor(nameof(IsPaginationVisible))]
    [NotifyPropertyChangedFor(nameof(PageJumpWatermark))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int _selectedPageSize = 25;

    [ObservableProperty] private string _historyTitle = string.Empty;
    [ObservableProperty] private string _historySubtitle = string.Empty;
    [ObservableProperty] private string _searchDateWatermark = string.Empty;
    [ObservableProperty] private string _logDetailSubtitle = string.Empty;
    [ObservableProperty] private string _searchLogWatermark = string.Empty;

    [ObservableProperty] private string _colTime = string.Empty;
    [ObservableProperty] private string _colName = string.Empty;
    [ObservableProperty] private string _colStatus = string.Empty;
    [ObservableProperty] private string _colSource = string.Empty;
    [ObservableProperty] private string _colDest = string.Empty;
    [ObservableProperty] private string _colSize = string.Empty;
    [ObservableProperty] private string _colDuration = string.Empty;
    [ObservableProperty] private string _colEncryption = string.Empty;

    [ObservableProperty] private string _colBackupId = string.Empty;
    [ObservableProperty] private string _colRuns = string.Empty;
    [ObservableProperty] private string _colStart = string.Empty;
    [ObservableProperty] private string _colEnd = string.Empty;
    [ObservableProperty] private string _colTotalDuration = string.Empty;
    [ObservableProperty] private string _colTotalSize = string.Empty;
    [ObservableProperty] private string _colFormat = string.Empty;
    [ObservableProperty] private string _statusInProgress = string.Empty;
    [ObservableProperty] private string _statusCompleted = string.Empty;
    [ObservableProperty] private string _statusError = string.Empty;

    public int PageSize => Math.Max(1, SelectedPageSize);
    public int TotalPages => FilteredLogsCount == 0 ? 1 : (int)Math.Ceiling((double)FilteredLogsCount / PageSize);
    public string PageDisplay => $"{CurrentPage}/{TotalPages}";
    public string PageJumpWatermark => $"1-{TotalPages}";
    public bool IsJobListVisible => IsDateSelected && !IsBackupJobSelected;
    public bool IsRunListVisible => IsDateSelected && IsBackupJobSelected && !IsRunSelected;
    public bool IsRunEntriesVisible => IsDateSelected && IsRunSelected;
    public bool IsPaginationVisible => IsRunEntriesVisible && TotalPages > 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogViewModel"/> class.
    /// </summary>
    /// <param name="logQueryService">Service to query log files by date.</param>
    /// <param name="logNavigationService">Service to navigate logs by job and run.</param>
    /// <param name="localizationService">Service for text localization.</param>
    public LogViewModel(
        ILogQueryService logQueryService,
        ILogNavigationService logNavigationService,
        ILocalizationService localizationService)
    {
        _logQueryService = logQueryService;
        _logNavigationService = logNavigationService;
        _localizationService = localizationService;

        RefreshTranslations();
        LoadDates();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (IsRunEntriesVisible)
        {
            CurrentPage = 1;
        }

        Refresh();
    }

    partial void OnSearchTextDateChanged(string value) => Refresh();

    partial void OnSelectedPageSizeChanged(int value)
    {
        if (value <= 0)
        {
            SelectedPageSize = 25;
            return;
        }

        CurrentPage = 1;
        Refresh();
    }

    public void RefreshTranslations()
    {
        HistoryTitle = _localizationService.TranslateText(LocalizationKey.gui_sidebar_log);
        HistorySubtitle = _localizationService.TranslateText(LocalizationKey.gui_log_history_subtitle);
        SearchDateWatermark = _localizationService.TranslateText(LocalizationKey.gui_log_search_date_watermark);
        SearchLogWatermark = _localizationService.TranslateText(LocalizationKey.gui_log_search_log_watermark);

        ColTime = _localizationService.TranslateText(LocalizationKey.gui_log_col_time);
        ColName = _localizationService.TranslateText(LocalizationKey.backupjob_name);
        ColStatus = _localizationService.TranslateText(LocalizationKey.gui_log_col_status);
        ColSource = _localizationService.TranslateText(LocalizationKey.backupjob_source);
        ColDest = _localizationService.TranslateText(LocalizationKey.backupjob_destination);
        ColSize = _localizationService.TranslateText(LocalizationKey.gui_log_col_size);
        ColDuration = _localizationService.TranslateText(LocalizationKey.gui_log_col_duration);
        ColEncryption = _localizationService.TranslateText(LocalizationKey.gui_log_col_encryption);

        ColBackupId = _localizationService.TranslateText(LocalizationKey.backupjob_id);
        ColRuns = _localizationService.TranslateText(LocalizationKey.gui_log_col_runs);
        ColStart = _localizationService.TranslateText(LocalizationKey.gui_log_col_start);
        ColEnd = _localizationService.TranslateText(LocalizationKey.gui_log_col_end);
        ColTotalDuration = _localizationService.TranslateText(LocalizationKey.gui_log_col_total_duration);
        ColTotalSize = _localizationService.TranslateText(LocalizationKey.gui_log_col_total_size);
        ColFormat = _localizationService.TranslateText(LocalizationKey.gui_log_col_format);

        StatusInProgress = _localizationService.TranslateText(LocalizationKey.gui_log_status_in_progress);
        StatusCompleted = _localizationService.TranslateText(LocalizationKey.gui_log_status_completed);
        StatusError = _localizationService.TranslateText(LocalizationKey.gui_log_status_error);

        UpdateDetailSubtitle();
    }

    [RelayCommand]
    private void SelectDate(string date)
    {
        CurrentPage = 1;
        PageInputText = string.Empty;
        SearchText = string.Empty;

        SelectedDate = date;
        IsDateSelected = true;
        IsBackupJobSelected = false;
        IsRunSelected = false;
        SelectedBackupId = 0;
        SelectedBackupName = string.Empty;
        SelectedRunTitle = string.Empty;

        LoadBackupJobsForDate(date);
    }

    [RelayCommand]
    private void SelectBackupJob(LogJobSummaryModel job)
    {
        if (job is null || string.IsNullOrWhiteSpace(SelectedDate))
        {
            return;
        }

        CurrentPage = 1;
        PageInputText = string.Empty;
        SearchText = string.Empty;

        SelectedBackupId = job.BackupId;
        SelectedBackupName = job.BackupName;
        IsBackupJobSelected = true;
        IsRunSelected = false;
        SelectedRunTitle = string.Empty;

        LoadRunsForBackup(SelectedDate, job.BackupId);
    }

    [RelayCommand]
    private void SelectRun(LogRunSummaryModel run)
    {
        if (run is null || string.IsNullOrWhiteSpace(SelectedDate))
        {
            return;
        }

        CurrentPage = 1;
        PageInputText = string.Empty;
        SearchText = string.Empty;

        IsRunSelected = true;
        SelectedRunTitle = $"{run.StartTime} -> {run.EndTime}";

        LoadEntriesForRun(SelectedDate, run.RunId);
    }

    [RelayCommand]
    private void GoBack()
    {
        if (IsRunSelected)
        {
            IsRunSelected = false;
            SelectedRunTitle = string.Empty;
            SearchText = string.Empty;
            CurrentPage = 1;
            PageInputText = string.Empty;
            _allLogEntries.Clear();
            Refresh();
            return;
        }

        if (IsBackupJobSelected)
        {
            IsBackupJobSelected = false;
            SelectedBackupId = 0;
            SelectedBackupName = string.Empty;
            SearchText = string.Empty;
            _allRuns.Clear();
            _allLogEntries.Clear();
            Refresh();
            return;
        }

        IsDateSelected = false;
        SelectedDate = null;
        SearchText = string.Empty;
        PageInputText = string.Empty;
        CurrentPage = 1;
        _allBackupJobs.Clear();
        _allRuns.Clear();
        _allLogEntries.Clear();
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void PreviousPage()
    {
        if (!CanGoToPreviousPage())
        {
            return;
        }

        CurrentPage--;
        Refresh();
    }

    private bool CanGoToPreviousPage() => IsRunEntriesVisible && CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void NextPage()
    {
        if (!CanGoToNextPage())
        {
            return;
        }

        CurrentPage++;
        Refresh();
    }

    private bool CanGoToNextPage() => IsRunEntriesVisible && CurrentPage < TotalPages;

    [RelayCommand]
    private void GoToPage(int pageNumber)
    {
        if (!IsRunEntriesVisible || pageNumber < 1 || pageNumber > TotalPages || pageNumber == CurrentPage)
        {
            return;
        }

        CurrentPage = pageNumber;
        Refresh();
    }

    [RelayCommand]
    private void JumpToEnteredPage()
    {
        if (!IsRunEntriesVisible)
        {
            return;
        }

        if (!int.TryParse(PageInputText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var page))
        {
            return;
        }

        page = Math.Clamp(page, 1, TotalPages);
        PageInputText = string.Empty;

        if (page == CurrentPage)
        {
            return;
        }

        CurrentPage = page;
        Refresh();
    }

    [RelayCommand]
    private void LoadDates()
    {
        _allAvailableDates.Clear();
        var dates = _logQueryService.GetAvailableDates();

        foreach (var date in dates)
        {
            _allAvailableDates.Add(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        if (!_hasAppliedDefaultDateSelection && _allAvailableDates.Count > 0)
        {
            _hasAppliedDefaultDateSelection = true;
            SelectDate(_allAvailableDates[0]);
            return;
        }

        Refresh();
    }

    public void SetOnLanguageChanged(Action onLanguageChanged)
    {
        _onLanguageChanged = onLanguageChanged;
    }

    private void LoadBackupJobsForDate(string date)
    {
        _allBackupJobs.Clear();
        _allRuns.Clear();
        _allLogEntries.Clear();

        var parsedDate = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var jobs = _logNavigationService.GetJobsByDate(parsedDate);

        foreach (var job in jobs)
        {
            _allBackupJobs.Add(new LogJobSummaryModel
            {
                BackupId = job.BackupId,
                BackupName = job.BackupName,
                RunCount = job.RunCount
            });
        }

        UpdateDetailSubtitle();
        Refresh();
    }

    private void LoadRunsForBackup(string date, int backupId)
    {
        _allRuns.Clear();
        _allLogEntries.Clear();

        var parsedDate = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var runs = _logNavigationService.GetRunsByDateAndBackupId(parsedDate, backupId);

        foreach (var run in runs)
        {
            _allRuns.Add(new LogRunSummaryModel
            {
                RunId = run.RunId,
                BackupId = run.BackupId,
                BackupName = run.BackupName,
                StartTime = run.StartTimestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                EndTime = run.EndTimestamp?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? StatusInProgress,
                Status = GetRunStatusText(run.Status),
                StatusTone = GetRunStatusTone(run.Status),
                TotalDuration = GetRunTotalDurationText(run),
                TotalSize = run.TotalSizeBytes.HasValue
                    ? FormatFileSize(run.TotalSizeBytes.Value)
                    : "-",
                Format = run.Format.ToString().ToUpperInvariant(),
                IsInProgress = run.Status == LogRunStatus.InProgress,
                IsError = run.Status == LogRunStatus.Error
            });
        }

        UpdateDetailSubtitle();
        Refresh();
    }

    private void LoadEntriesForRun(string date, string runId)
    {
        _allLogEntries.Clear();

        var parsedDate = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var entries = _logNavigationService.GetEntriesByRun(parsedDate, runId);

        foreach (var log in entries)
        {
            _allLogEntries.Add(new LogDisplayModel
            {
                Timestamp = log.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                BackupName = log.BackupName,
                EventType = log.EventType.ToString(),
                Source = log.SourcePathUNC,
                Destination = log.DestinationPathUNC,
                FileSize = FormatFileSize(log.FileSizeBytes),
                Duration = $"{log.TransferTimeMs} ms",
                EncryptionTime = FormatEncryptionTime(log.EncryptionTimeMs)
            });
        }

        UpdateDetailSubtitle();
        Refresh();
    }

    private void Refresh()
    {
        RefreshDates();

        BackupJobs.Clear();
        Runs.Clear();
        LogEntries.Clear();

        var query = SearchText.Trim();

        if (IsJobListVisible)
        {
            foreach (var job in _allBackupJobs.Where(job =>
                         string.IsNullOrEmpty(query) ||
                         job.BackupName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         job.BackupId.ToString(CultureInfo.InvariantCulture).Contains(query, StringComparison.OrdinalIgnoreCase)))
            {
                BackupJobs.Add(job);
            }

            FilteredLogsCount = 0;
            PaginationItems.Clear();
            return;
        }

        if (IsRunListVisible)
        {
            foreach (var run in _allRuns.Where(run =>
                         string.IsNullOrEmpty(query) ||
                         run.StartTime.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         run.EndTime.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         run.Status.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         run.TotalDuration.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         run.TotalSize.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         run.Format.Contains(query, StringComparison.OrdinalIgnoreCase)))
            {
                Runs.Add(run);
            }

            FilteredLogsCount = 0;
            PaginationItems.Clear();
            return;
        }

        if (IsRunEntriesVisible)
        {
            var filteredLogs = _allLogEntries.Where(l =>
                string.IsNullOrEmpty(query) ||
                l.Source.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                l.Destination.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                l.EventType.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            FilteredLogsCount = filteredLogs.Count;

            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
            else if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            var skipCount = (CurrentPage - 1) * PageSize;
            foreach (var entry in filteredLogs.Skip(skipCount).Take(PageSize))
            {
                LogEntries.Add(entry);
            }

            RebuildPaginationItems();
            return;
        }

        FilteredLogsCount = 0;
        PaginationItems.Clear();
    }

    private void RefreshDates()
    {
        AvailableDates.Clear();
        var dateQuery = SearchTextDate.Trim();
        var filteredDates = _allAvailableDates.Where(d =>
            string.IsNullOrEmpty(dateQuery) || d.Contains(dateQuery, StringComparison.OrdinalIgnoreCase));

        foreach (var date in filteredDates)
        {
            AvailableDates.Add(date);
        }
    }

    private void UpdateDetailSubtitle()
    {
        if (IsRunEntriesVisible)
        {
            LogDetailSubtitle = _localizationService.TranslateText(LocalizationKey.gui_log_entries_subtitle);
        }
        else if (IsRunListVisible)
        {
            LogDetailSubtitle = _localizationService.TranslateText(LocalizationKey.gui_log_runs_subtitle);
        }
        else if (IsJobListVisible)
        {
            LogDetailSubtitle = _localizationService.TranslateText(LocalizationKey.gui_log_jobs_subtitle);
        }
        else
        {
            LogDetailSubtitle = _localizationService.TranslateText(LocalizationKey.gui_log_detail_subtitle);
        }
    }

    partial void OnIsDateSelectedChanged(bool value)
    {
        UpdateDetailSubtitle();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBackupJobSelectedChanged(bool value)
    {
        UpdateDetailSubtitle();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsRunSelectedChanged(bool value)
    {
        UpdateDetailSubtitle();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    private void RebuildPaginationItems()
    {
        PaginationItems.Clear();
        foreach (var item in BuildVisibleItems(CurrentPage, TotalPages))
        {
            PaginationItems.Add(item);
        }
    }

    private static List<PaginationItem> BuildVisibleItems(int currentPage, int totalPages)
    {
        if (totalPages <= 0)
        {
            return [];
        }

        if (totalPages <= 7)
        {
            var items = new List<PaginationItem>(totalPages);
            for (var page = 1; page <= totalPages; page++)
            {
                items.Add(PaginationItem.Page(page, page == currentPage));
            }

            return items;
        }

        if (currentPage <= 4)
        {
            return
            [
                PaginationItem.Page(1, currentPage == 1),
                PaginationItem.Page(2, currentPage == 2),
                PaginationItem.Page(3, currentPage == 3),
                PaginationItem.Page(4, currentPage == 4),
                PaginationItem.Page(5, currentPage == 5),
                PaginationItem.Ellipsis(),
                PaginationItem.Page(totalPages, currentPage == totalPages)
            ];
        }

        if (currentPage >= totalPages - 3)
        {
            return
            [
                PaginationItem.Page(1, currentPage == 1),
                PaginationItem.Ellipsis(),
                PaginationItem.Page(totalPages - 4, currentPage == totalPages - 4),
                PaginationItem.Page(totalPages - 3, currentPage == totalPages - 3),
                PaginationItem.Page(totalPages - 2, currentPage == totalPages - 2),
                PaginationItem.Page(totalPages - 1, currentPage == totalPages - 1),
                PaginationItem.Page(totalPages, currentPage == totalPages)
            ];
        }

        return
        [
            PaginationItem.Page(1, currentPage == 1),
            PaginationItem.Ellipsis(),
            PaginationItem.Page(currentPage - 1, false),
            PaginationItem.Page(currentPage, true),
            PaginationItem.Page(currentPage + 1, false),
            PaginationItem.Ellipsis(),
            PaginationItem.Page(totalPages, currentPage == totalPages)
        ];
    }

    private string FormatEncryptionTime(long timeMs)
    {
        if (timeMs == 0)
        {
            return "0 ms";
        }

        if (timeMs > 0)
        {
            return $"{timeMs} ms";
        }

        return "Error";
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int i = 0;

        while (value >= 1024 && i < units.Length - 1)
        {
            value /= 1024;
            i++;
        }

        return $"{value:0.##} {units[i]}";
    }

    private string GetRunStatusText(LogRunStatus status)
    {
        return status switch
        {
            LogRunStatus.Completed => StatusCompleted,
            LogRunStatus.Error => StatusError,
            _ => StatusInProgress
        };
    }

    private string GetRunTotalDurationText(LogRunSummary run)
    {
        return run.Status switch
        {
            LogRunStatus.InProgress => StatusInProgress,
            LogRunStatus.Error => "-",
            _ => run.TotalDurationMs.HasValue ? $"{run.TotalDurationMs.Value} ms" : "-"
        };
    }

    private static string GetRunStatusTone(LogRunStatus status)
    {
        return status switch
        {
            LogRunStatus.Completed => "completed",
            LogRunStatus.Error => "error",
            _ => "inprogress"
        };
    }
}
