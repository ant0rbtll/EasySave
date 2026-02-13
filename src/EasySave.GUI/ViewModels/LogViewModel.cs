using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Application;
using EasySave.GUI.Helpers;
using EasySave.GUI.Models;
using EasySave.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace EasySave.GUI.ViewModels;

/// <summary>
/// Single-page log navigation view model: calendar, job accordion and run details drawer.
/// </summary>
public partial class LogViewModel : ViewModelBase
{
    private readonly ILogQueryService _logQueryService;
    private readonly ILogNavigationService _logNavigationService;
    private readonly ILocalizationService _localizationService;

    private readonly List<DateOnly> _allAvailableDates = [];
    private readonly HashSet<DateOnly> _availableDateLookup = [];
    private readonly List<LogJobSummaryModel> _allBackupJobs = [];
    private readonly List<LogDisplayModel> _allLogEntries = [];

    private DateOnly? _selectedDateValue;
    private DateOnly _calendarMonth;
    private string _selectedRunId = string.Empty;
    private bool _isUpdatingYearPicker;

    public ObservableCollection<LogCalendarDayModel> CalendarDays { get; } = [];
    public ObservableCollection<LogYearPickerItemModel> YearPickerItems { get; } = [];
    public ObservableCollection<LogMonthPickerItemModel> MonthPickerItems { get; } = [];
    public ObservableCollection<LogJobSummaryModel> BackupJobs { get; } = [];
    public ObservableCollection<LogDisplayModel> LogEntries { get; } = [];
    public ObservableCollection<PaginationItem> PaginationItems { get; } = [];

    public IReadOnlyList<int> PageSizeOptions { get; } = [15, 25, 50];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedDate = string.Empty;
    [ObservableProperty] private string _calendarMonthTitle = string.Empty;
    [ObservableProperty] private LogYearPickerItemModel? _selectedYearItem;

    [ObservableProperty] private string _historyTitle = string.Empty;
    [ObservableProperty] private string _historySubtitle = string.Empty;
    [ObservableProperty] private string _searchLogWatermark = string.Empty;
    [ObservableProperty] private string _logDetailSubtitle = string.Empty;

    [ObservableProperty] private string _colTime = string.Empty;
    [ObservableProperty] private string _colBackupState = string.Empty;
    [ObservableProperty] private string _colStatus = string.Empty;
    [ObservableProperty] private string _colSource = string.Empty;
    [ObservableProperty] private string _colDest = string.Empty;
    [ObservableProperty] private string _colSize = string.Empty;
    [ObservableProperty] private string _colDuration = string.Empty;
    [ObservableProperty] private string _colEncryption = string.Empty;
    [ObservableProperty] private string _colStart = string.Empty;
    [ObservableProperty] private string _colEnd = string.Empty;
    [ObservableProperty] private string _colTotalDuration = string.Empty;
    [ObservableProperty] private string _colTotalSize = string.Empty;
    [ObservableProperty] private string _colFormat = string.Empty;
    [ObservableProperty] private string _colRuns = string.Empty;

    [ObservableProperty] private string _statusInProgress = string.Empty;
    [ObservableProperty] private string _statusCompleted = string.Empty;
    [ObservableProperty] private string _statusError = string.Empty;

    [ObservableProperty] private string _selectedBackupName = string.Empty;
    [ObservableProperty] private string _selectedRunTitle = string.Empty;
    [ObservableProperty] private string _selectedRunFormat = string.Empty;
    [ObservableProperty] private string _selectedRunStatus = string.Empty;
    [ObservableProperty] private string _selectedRunStatusTone = "inprogress";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunDrawerVisible))]
    [NotifyPropertyChangedFor(nameof(IsPaginationVisible))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private bool _isRunSelected;

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

    public int PageSize => Math.Max(1, SelectedPageSize);
    public int TotalPages => FilteredLogsCount == 0 ? 1 : (int)Math.Ceiling((double)FilteredLogsCount / PageSize);
    public string PageDisplay => $"{CurrentPage}/{TotalPages}";
    public string PageJumpWatermark => $"1-{TotalPages}";
    public bool IsRunDrawerVisible => IsRunSelected;
    public bool IsPaginationVisible => IsRunDrawerVisible && TotalPages > 1;

    public LogViewModel(
        ILogQueryService logQueryService,
        ILogNavigationService logNavigationService,
        ILocalizationService localizationService)
    {
        _logQueryService = logQueryService;
        _logNavigationService = logNavigationService;
        _localizationService = localizationService;

        _calendarMonth = DateOnly.FromDateTime(DateTime.Today).AddDays(1 - DateTime.Today.Day);

        RefreshTranslations();
        LoadDates();
    }

    partial void OnSearchTextChanged(string value) => RefreshJobs();

    partial void OnSelectedYearItemChanged(LogYearPickerItemModel? value)
    {
        if (_isUpdatingYearPicker || value is null)
        {
            return;
        }

        SelectYear(value);
    }

    partial void OnSelectedPageSizeChanged(int value)
    {
        if (value <= 0)
        {
            SelectedPageSize = 25;
            return;
        }

        CurrentPage = 1;
        RefreshRunEntries();
    }

    partial void OnIsRunSelectedChanged(bool value)
    {
        if (!value)
        {
            CurrentPage = 1;
            PageInputText = string.Empty;
            _selectedRunId = string.Empty;
            _allLogEntries.Clear();
            LogEntries.Clear();
            FilteredLogsCount = 0;
            PaginationItems.Clear();
            SelectedBackupName = string.Empty;
            SelectedRunTitle = string.Empty;
            SelectedRunFormat = string.Empty;
            SelectedRunStatus = string.Empty;
            SelectedRunStatusTone = "inprogress";
        }

        UpdateDetailSubtitle();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    public void RefreshTranslations()
    {
        HistoryTitle = _localizationService.TranslateText(LocalizationKey.gui_sidebar_log);
        HistorySubtitle = _localizationService.TranslateText(LocalizationKey.gui_log_history_subtitle);
        SearchLogWatermark = _localizationService.TranslateText(LocalizationKey.gui_log_search_log_watermark);

        ColTime = _localizationService.TranslateText(LocalizationKey.gui_log_col_time);
        ColBackupState = _localizationService.TranslateText(LocalizationKey.gui_log_col_backup_state);
        ColStatus = _localizationService.TranslateText(LocalizationKey.gui_log_col_status);
        ColSource = _localizationService.TranslateText(LocalizationKey.backupjob_source);
        ColDest = _localizationService.TranslateText(LocalizationKey.backupjob_destination);
        ColSize = _localizationService.TranslateText(LocalizationKey.gui_log_col_size);
        ColDuration = _localizationService.TranslateText(LocalizationKey.gui_log_col_duration);
        ColEncryption = _localizationService.TranslateText(LocalizationKey.gui_log_col_encryption);
        ColStart = _localizationService.TranslateText(LocalizationKey.gui_log_col_start);
        ColEnd = _localizationService.TranslateText(LocalizationKey.gui_log_col_end);
        ColTotalDuration = _localizationService.TranslateText(LocalizationKey.gui_log_col_total_duration);
        ColTotalSize = _localizationService.TranslateText(LocalizationKey.gui_log_col_total_size);
        ColFormat = _localizationService.TranslateText(LocalizationKey.gui_log_col_format);
        ColRuns = _localizationService.TranslateText(LocalizationKey.gui_log_col_runs);

        StatusInProgress = _localizationService.TranslateText(LocalizationKey.gui_log_status_in_progress);
        StatusCompleted = _localizationService.TranslateText(LocalizationKey.gui_log_status_completed);
        StatusError = _localizationService.TranslateText(LocalizationKey.gui_log_status_error);

        BuildYearPickerItems();
        BuildMonthPickerItems();
        BuildCalendarDays();
        UpdateCalendarMonthTitle();
        UpdateDetailSubtitle();

        if (_selectedDateValue.HasValue)
        {
            LoadBackupJobsForDate(_selectedDateValue.Value);
        }
    }

    [RelayCommand]
    private void LoadDates()
    {
        _allAvailableDates.Clear();
        _availableDateLookup.Clear();

        foreach (var date in _logQueryService.GetAvailableDates())
        {
            _allAvailableDates.Add(date);
            _availableDateLookup.Add(date);
        }

        var targetDate = _selectedDateValue;
        if (!targetDate.HasValue || !_availableDateLookup.Contains(targetDate.Value))
        {
            targetDate = _allAvailableDates.FirstOrDefault();
        }

        if (!targetDate.HasValue || targetDate.Value == default)
        {
            _selectedDateValue = null;
            SelectedDate = string.Empty;
            _allBackupJobs.Clear();
            BackupJobs.Clear();
            IsRunSelected = false;
            BuildYearPickerItems();
            BuildMonthPickerItems();
            BuildCalendarDays();
            UpdateDetailSubtitle();
            return;
        }

        SelectDateInternal(targetDate.Value, true);
    }

    [RelayCommand]
    private void SelectYear(LogYearPickerItemModel? yearItem)
    {
        if (yearItem is null)
        {
            return;
        }

        var datesInYear = _allAvailableDates
            .Where(d => d.Year == yearItem.Year)
            .OrderByDescending(d => d)
            .ToList();

        if (datesInYear.Count == 0)
        {
            return;
        }

        var targetDate = _selectedDateValue.HasValue && _selectedDateValue.Value.Year == yearItem.Year
            ? _selectedDateValue.Value
            : datesInYear[0];

        SelectDateInternal(targetDate, true);
    }

    [RelayCommand]
    private void SelectMonth(LogMonthPickerItemModel? monthItem)
    {
        if (monthItem is null || !monthItem.HasLogs)
        {
            return;
        }

        var selectedYear = _calendarMonth.Year;
        var datesInMonth = _allAvailableDates
            .Where(d => d.Year == selectedYear && d.Month == monthItem.Month)
            .OrderByDescending(d => d)
            .ToList();

        if (datesInMonth.Count == 0)
        {
            return;
        }

        var targetDate = _selectedDateValue.HasValue &&
                         _selectedDateValue.Value.Year == selectedYear &&
                         _selectedDateValue.Value.Month == monthItem.Month
            ? _selectedDateValue.Value
            : datesInMonth[0];

        SelectDateInternal(targetDate, true);
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        _calendarMonth = _calendarMonth.AddMonths(-1);
        BuildCalendarDays();
    }

    [RelayCommand]
    private void NextMonth()
    {
        _calendarMonth = _calendarMonth.AddMonths(1);
        BuildCalendarDays();
    }

    [RelayCommand]
    private void SelectCalendarDay(LogCalendarDayModel? day)
    {
        if (day is null || !day.HasLogs)
        {
            return;
        }

        SelectDateInternal(day.Date, true);
    }

    [RelayCommand]
    private void SelectRun(LogRunSummaryModel? run)
    {
        if (run is null || !_selectedDateValue.HasValue)
        {
            return;
        }

        _selectedRunId = run.RunId;
        SelectedBackupName = run.BackupName;
        SelectedRunTitle = $"{run.StartTime} -> {run.EndTime}";
        SelectedRunFormat = run.Format;
        SelectedRunStatus = run.Status;
        SelectedRunStatusTone = run.StatusTone;

        IsRunSelected = true;
        CurrentPage = 1;
        PageInputText = string.Empty;

        LoadEntriesForRun(_selectedDateValue.Value, run.RunId);
    }

    [RelayCommand]
    private void CloseRunDetails()
    {
        IsRunSelected = false;
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void PreviousPage()
    {
        if (!CanGoToPreviousPage())
        {
            return;
        }

        CurrentPage--;
        RefreshRunEntries();
    }

    private bool CanGoToPreviousPage() => IsRunDrawerVisible && CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void NextPage()
    {
        if (!CanGoToNextPage())
        {
            return;
        }

        CurrentPage++;
        RefreshRunEntries();
    }

    private bool CanGoToNextPage() => IsRunDrawerVisible && CurrentPage < TotalPages;

    [RelayCommand]
    private void GoToPage(int pageNumber)
    {
        if (!IsRunDrawerVisible || pageNumber < 1 || pageNumber > TotalPages || pageNumber == CurrentPage)
        {
            return;
        }

        CurrentPage = pageNumber;
        RefreshRunEntries();
    }

    [RelayCommand]
    private void JumpToEnteredPage()
    {
        if (!IsRunDrawerVisible)
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
        RefreshRunEntries();
    }

    public void SetOnLanguageChanged(Action onLanguageChanged)
    {
        _ = onLanguageChanged;
    }

    private void SelectDateInternal(DateOnly date, bool reloadJobs)
    {
        _selectedDateValue = date;
        SelectedDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        _calendarMonth = date.AddDays(1 - date.Day);

        BuildYearPickerItems();
        BuildMonthPickerItems();
        BuildCalendarDays();

        if (reloadJobs)
        {
            LoadBackupJobsForDate(date);
        }
    }

    private void BuildCalendarDays()
    {
        CalendarDays.Clear();
        UpdateCalendarMonthTitle();

        var firstDayOfMonth = _calendarMonth;
        var dayOffset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;
        var gridStart = firstDayOfMonth.AddDays(-dayOffset);

        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i);
            CalendarDays.Add(new LogCalendarDayModel
            {
                Date = date,
                DayText = date.Day.ToString(CultureInfo.InvariantCulture),
                IsCurrentMonth = date.Month == firstDayOfMonth.Month && date.Year == firstDayOfMonth.Year,
                HasLogs = _availableDateLookup.Contains(date),
                IsSelected = _selectedDateValue.HasValue && date == _selectedDateValue.Value
            });
        }
    }

    private void UpdateCalendarMonthTitle()
    {
        CalendarMonthTitle = _calendarMonth.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", CultureInfo.CurrentCulture);
    }

    private void BuildYearPickerItems()
    {
        YearPickerItems.Clear();

        var selectedYear = _calendarMonth.Year;
        foreach (var year in _allAvailableDates
                     .Select(d => d.Year)
                     .Distinct()
                     .OrderByDescending(y => y))
        {
            YearPickerItems.Add(new LogYearPickerItemModel
            {
                Year = year,
                IsSelected = year == selectedYear
            });
        }

        _isUpdatingYearPicker = true;
        SelectedYearItem = YearPickerItems.FirstOrDefault(y => y.IsSelected) ?? YearPickerItems.FirstOrDefault();
        _isUpdatingYearPicker = false;
    }

    private void BuildMonthPickerItems()
    {
        MonthPickerItems.Clear();

        var selectedYear = _calendarMonth.Year;
        var selectedMonth = _calendarMonth.Month;
        var culture = CultureInfo.CurrentCulture;

        for (var month = 1; month <= 12; month++)
        {
            var monthLabel = new DateTime(2000, month, 1).ToString("MMM", culture);
            var hasLogs = _allAvailableDates.Any(d => d.Year == selectedYear && d.Month == month);

            MonthPickerItems.Add(new LogMonthPickerItemModel
            {
                Month = month,
                Label = monthLabel,
                HasLogs = hasLogs,
                IsSelected = month == selectedMonth
            });
        }
    }

    private void LoadBackupJobsForDate(DateOnly date)
    {
        var previouslySelectedRunId = _selectedRunId;

        _allBackupJobs.Clear();

        var jobs = _logNavigationService.GetJobsByDate(date);
        foreach (var job in jobs)
        {
            var mappedRuns = _logNavigationService
                .GetRunsByDateAndBackupId(date, job.BackupId)
                .Select(MapRun)
                .ToList();

            _allBackupJobs.Add(new LogJobSummaryModel
            {
                BackupId = job.BackupId,
                BackupName = job.BackupName,
                RunCount = mappedRuns.Count,
                RunsSubtitle = BuildRunsSubtitle(mappedRuns.Count),
                IsExpanded = false,
                Runs = mappedRuns
            });
        }

        RefreshJobs();
        ReSelectRun(previouslySelectedRunId);
        UpdateDetailSubtitle();
    }

    private void ReSelectRun(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            IsRunSelected = false;
            return;
        }

        var run = _allBackupJobs.SelectMany(static j => j.Runs).FirstOrDefault(r => r.RunId == runId);
        if (run is null)
        {
            IsRunSelected = false;
            return;
        }

        SelectRun(run);
    }

    private void LoadEntriesForRun(DateOnly date, string runId)
    {
        _allLogEntries.Clear();

        var entries = _logNavigationService.GetEntriesByRun(date, runId);
        foreach (var log in entries)
        {
            _allLogEntries.Add(new LogDisplayModel
            {
                Timestamp = log.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                BackupName = log.BackupName,
                EventType = log.EventType.ToString(),
                Source = log.SourcePathUNC,
                Destination = log.DestinationPathUNC,
                FileSize = LogValueFormatter.FormatFileSize(log.FileSizeBytes),
                Duration = LogValueFormatter.FormatDuration(log.TransferTimeMs),
                EncryptionTime = log.EncryptionTimeMs < 0
                    ? StatusError
                    : LogValueFormatter.FormatDuration(log.EncryptionTimeMs)
            });
        }

        RefreshRunEntries();
        UpdateDetailSubtitle();
    }

    private void RefreshJobs()
    {
        BackupJobs.Clear();
        var query = SearchText.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            foreach (var job in _allBackupJobs)
            {
                BackupJobs.Add(job);
            }

            return;
        }

        foreach (var job in _allBackupJobs)
        {
            var jobMatch =
                job.BackupName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                job.BackupId.ToString(CultureInfo.InvariantCulture).Contains(query, StringComparison.OrdinalIgnoreCase);

            var matchingRuns = job.Runs.Where(run =>
                    run.StartTime.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    run.EndTime.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    run.Status.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    run.TotalDuration.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    run.TotalSize.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!jobMatch && matchingRuns.Count == 0)
            {
                continue;
            }

            BackupJobs.Add(new LogJobSummaryModel
            {
                BackupId = job.BackupId,
                BackupName = job.BackupName,
                RunCount = job.RunCount,
                RunsSubtitle = job.RunsSubtitle,
                IsExpanded = true,
                Runs = jobMatch ? job.Runs : matchingRuns
            });
        }
    }

    private void RefreshRunEntries()
    {
        LogEntries.Clear();

        if (!IsRunDrawerVisible)
        {
            FilteredLogsCount = 0;
            PaginationItems.Clear();
            return;
        }

        FilteredLogsCount = _allLogEntries.Count;

        if (CurrentPage > TotalPages)
        {
            CurrentPage = TotalPages;
        }
        else if (CurrentPage < 1)
        {
            CurrentPage = 1;
        }

        var skipCount = (CurrentPage - 1) * PageSize;
        foreach (var entry in _allLogEntries.Skip(skipCount).Take(PageSize))
        {
            LogEntries.Add(entry);
        }

        RebuildPaginationItems();
    }

    private void UpdateDetailSubtitle()
    {
        if (string.IsNullOrWhiteSpace(SelectedDate))
        {
            LogDetailSubtitle = _localizationService.TranslateText(LocalizationKey.gui_log_history_subtitle);
            return;
        }

        if (IsRunDrawerVisible)
        {
            LogDetailSubtitle = _localizationService.TranslateText(LocalizationKey.gui_log_entries_subtitle);
            return;
        }

        LogDetailSubtitle = _localizationService.TranslateText(LocalizationKey.gui_log_runs_subtitle);
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

    private LogRunSummaryModel MapRun(LogRunSummary run)
    {
        return new LogRunSummaryModel
        {
            RunId = run.RunId,
            BackupId = run.BackupId,
            BackupName = run.BackupName,
            StartTime = run.StartTimestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            EndTime = run.EndTimestamp?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? StatusInProgress,
            Status = GetRunStatusText(run.Status),
            StatusTone = GetRunStatusTone(run.Status),
            TotalDuration = GetRunTotalDurationText(run),
            TotalSize = run.Status == LogRunStatus.Completed && run.TotalSizeBytes.HasValue
                ? LogValueFormatter.FormatFileSize(run.TotalSizeBytes.Value)
                : string.Empty,
            Format = run.Format.ToString().ToUpperInvariant(),
            IsInProgress = run.Status == LogRunStatus.InProgress,
            IsError = run.Status == LogRunStatus.Error
        };
    }

    private string BuildRunsSubtitle(int runCount)
    {
        return $"{runCount} {ColRuns.ToLowerInvariant()}";
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
            _ => run.TotalDurationMs.HasValue ? LogValueFormatter.FormatDuration(run.TotalDurationMs.Value) : "-"
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
