using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Application;
using EasySave.Core;
using EasySave.GUI.Models;
using EasySave.Localization;
using EasySave.Log;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace EasySave.GUI.ViewModels;

/// <summary>
/// ViewModel for the Log view, handling date selection, log filtering, and navigation.
/// </summary>
public partial class LogViewModel : ViewModelBase
{
    private readonly ILogQueryService _logQueryService;
    private readonly ILocalizationService _localizationService;
    private readonly List<string> _allAvailableDates = new();
    private readonly List<LogDisplayModel> _allLogEntries = new();

    public ObservableCollection<string> AvailableDates { get; } = new();
    public ObservableCollection<LogDisplayModel> LogEntries { get; } = new();
    public ObservableCollection<PaginationItem> PaginationItems { get; } = new();
    public IReadOnlyList<int> PageSizeOptions { get; } = [15, 25, 50];
    private Action _onLanguageChanged = static () => { };

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _searchTextDate = string.Empty;
    [ObservableProperty] private string? _selectedDate;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPaginationVisible))]
    private bool _isDateSelected;
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
    public bool IsPaginationVisible => IsDateSelected && TotalPages > 1;

    [ObservableProperty] private string _historyTitle = "";
    [ObservableProperty] private string _historySubtitle = "";
    [ObservableProperty] private string _searchDateWatermark = "";
    [ObservableProperty] private string _logDetailSubtitle = "";
    [ObservableProperty] private string _searchLogWatermark = "";
    [ObservableProperty] private string _colTime = "";
    [ObservableProperty] private string _colName = "";
    [ObservableProperty] private string _colStatus = "";
    [ObservableProperty] private string _colSource = "";
    [ObservableProperty] private string _colDest = "";
    [ObservableProperty] private string _colSize = "";
    [ObservableProperty] private string _colDuration = "";
    [ObservableProperty] private string _colEncryption = "";

    /// <summary>
    /// Initializes a new instance of the <see cref="LogViewModel"/> class.
    /// </summary>
    /// <param name="logQueryService">Service to query log files.</param>
    /// <param name="localizationService">Service for text localization.</param>
    public LogViewModel(ILogQueryService logQueryService, ILocalizationService localizationService)
    {
        _logQueryService = logQueryService;
        _localizationService = localizationService;

        RefreshTranslations();
        LoadDates();
    }

    /// <summary>
    /// Triggered when the log search text changes to refresh the filtered list.
    /// </summary>
    /// <param name="value">The new search string.</param>
    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 1;
        Refresh();
    }

    /// <summary>
    /// Triggered when the date search text changes to refresh the filtered list.
    /// </summary>
    /// <param name="value">The new search string.</param>
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

    /// <summary>
    /// Updates all localized strings based on the current language from the localization service.
    /// </summary>
    public void RefreshTranslations()
    {
        HistoryTitle = _localizationService.TranslateText(LocalizationKey.gui_sidebar_log);
        HistorySubtitle = _localizationService.TranslateText(LocalizationKey.gui_log_history_subtitle);
        LogDetailSubtitle = _localizationService.TranslateText(LocalizationKey.gui_log_detail_subtitle);

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
    }

    /// <summary>
    /// Selects a specific date, updates the state, and loads the corresponding log entries.
    /// </summary>
    /// <param name="date">The date string selected by the user.</param>
    [RelayCommand]
    private void SelectDate(string date)
    {
        CurrentPage = 1;
        PageInputText = string.Empty;
        SelectedDate = date;
        IsDateSelected = true;
        LoadLogsForDate(date);
    }

    /// <summary>
    /// Returns to the date selection screen and clears the current log selection and search filters.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        IsDateSelected = false;
        SelectedDate = null;
        SearchText = string.Empty;
        PageInputText = string.Empty;
        CurrentPage = 1;
        _allLogEntries.Clear();
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void PreviousPage()
    {
        if (CurrentPage <= 1)
            return;

        CurrentPage--;
        Refresh();
    }

    private bool CanGoToPreviousPage() => CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void NextPage()
    {
        if (CurrentPage >= TotalPages)
            return;

        CurrentPage++;
        Refresh();
    }

    private bool CanGoToNextPage() => CurrentPage < TotalPages;

    [RelayCommand]
    private void GoToPage(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > TotalPages || pageNumber == CurrentPage)
            return;

        CurrentPage = pageNumber;
        Refresh();
    }

    [RelayCommand]
    private void JumpToEnteredPage()
    {
        if (!int.TryParse(PageInputText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var page))
            return;

        page = Math.Clamp(page, 1, TotalPages);
        PageInputText = string.Empty;

        if (page == CurrentPage)
            return;

        CurrentPage = page;
        Refresh();
    }

    /// <summary>
    /// Filters available dates and log entries based on their respective search text values.
    /// </summary>
    private void Refresh()
    {
        AvailableDates.Clear();
        var dateQuery = SearchTextDate.Trim();
        var filteredDates = _allAvailableDates.Where(d =>
            string.IsNullOrEmpty(dateQuery) || d.Contains(dateQuery, StringComparison.OrdinalIgnoreCase));

        foreach (var date in filteredDates)
            AvailableDates.Add(date);

        LogEntries.Clear();
        var logQuery = SearchText.Trim();
        var filteredLogs = _allLogEntries.Where(l =>
            string.IsNullOrEmpty(logQuery) ||
            l.BackupName.Contains(logQuery, StringComparison.OrdinalIgnoreCase) ||
            l.Source.Contains(logQuery, StringComparison.OrdinalIgnoreCase) ||
            l.Destination.Contains(logQuery, StringComparison.OrdinalIgnoreCase) ||
            l.EventType.Contains(logQuery, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        FilteredLogsCount = filteredLogs.Count;

        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;
        else if (CurrentPage < 1)
            CurrentPage = 1;

        var skipCount = (CurrentPage - 1) * PageSize;
        foreach (var entry in filteredLogs.Skip(skipCount).Take(PageSize))
            LogEntries.Add(entry);

        RebuildPaginationItems();
    }

    /// <summary>
    /// Fetches all available log dates from the log query service and updates the internal list.
    /// </summary>
    [RelayCommand]
    private void LoadDates()
    {
        _allAvailableDates.Clear();
        var dates = _logQueryService.GetAvailableDates();

        foreach (var date in dates)
        {
            _allAvailableDates.Add(date.ToString("yyyy-MM-dd"));
        }

        Refresh();
    }

    /// <summary>
    /// Sets the callback action to be executed when the application language changes.
    /// </summary>
    /// <param name="onLanguageChanged">The action to execute.</param>
    public void SetOnLanguageChanged(Action onLanguageChanged)
    {
        _onLanguageChanged = onLanguageChanged;
    }

    /// <summary>
    /// Loads log entries for a specific date from the log service and maps them to the display model.
    /// </summary>
    /// <param name="date">The date of the logs to load (formatted as yyyy-MM-dd).</param>
    private void LoadLogsForDate(string date)
    {
        _allLogEntries.Clear();
        var rawLogs = _logQueryService.GetByDate(DateOnly.Parse(date));

        foreach (var log in rawLogs)
        {
            _allLogEntries.Add(new LogDisplayModel
            {
                Timestamp = log.Timestamp.ToString("HH:mm:ss"),
                BackupName = log.BackupName,
                EventType = log.EventType.ToString(),
                Source = log.SourcePathUNC,
                Destination = log.DestinationPathUNC,
                FileSize = FormatFileSize(log.FileSizeBytes),
                Duration = $"{log.TransferTimeMs} ms",
                EncryptionTime = FormatEncryptionTime(log.EncryptionTimeMs)
            });
        }

        Refresh();
    }

    private void RebuildPaginationItems()
    {
        PaginationItems.Clear();
        foreach (var item in BuildVisibleItems(CurrentPage, TotalPages))
            PaginationItems.Add(item);
    }

    private static List<PaginationItem> BuildVisibleItems(int currentPage, int totalPages)
    {
        if (totalPages <= 0)
            return [];

        if (totalPages <= 7)
        {
            var items = new List<PaginationItem>(totalPages);
            for (var page = 1; page <= totalPages; page++)
                items.Add(PaginationItem.Page(page, page == currentPage));
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

    /// <summary>
    /// Formats the encryption time based on business rules: 0 for no encryption, positive for duration, negative for errors.
    /// </summary>
    /// <param name="timeMs">The encryption time in milliseconds.</param>
    /// <returns>A formatted string representing the encryption status or duration.</returns>
    private string FormatEncryptionTime(long timeMs)
    {
        if (timeMs == 0) return "0 ms";
        if (timeMs > 0) return $"{timeMs} ms";
        return "Error";
    }

    /// <summary>
    /// Converts a file size in bytes to a human-readable string with appropriate units (B, KB, MB, GB, TB).
    /// </summary>
    /// <param name="bytes">The file size in bytes.</param>
    /// <returns>A string representation of the file size (e.g., "1.25 MB").</returns>
    private string FormatFileSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double doubleBytes = bytes;
        int i = 0;
        while (doubleBytes >= 1024 && i < units.Length - 1)
        {
            doubleBytes /= 1024;
            i++;
        }
        return $"{doubleBytes:0.##} {units[i]}";
    }
}
