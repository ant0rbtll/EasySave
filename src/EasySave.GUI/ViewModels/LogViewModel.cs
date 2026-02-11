using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Application;
using EasySave.GUI.Models;
using EasySave.Localization;
using EasySave.Log;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EasySave.GUI.ViewModels;

/// <summary>
/// ViewModel for the Log view, handling date selection, log filtering, and navigation.
/// </summary>
public partial class LogViewModel : ViewModelBase
{
    private readonly BackupApplicationService _backupService;
    private readonly ILocalizationService _localizationService;
    private readonly List<string> _allAvailableDates = new();
    private readonly List<LogDisplayModel> _allLogEntries = new();
    public ObservableCollection<string> AvailableDates { get; } = new();
    public ObservableCollection<LogDisplayModel> LogEntries { get; } = new();
    private Action _onLanguageChanged = static () => { };

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _searchTextDate = string.Empty;
    [ObservableProperty] private string? _selectedDate;
    [ObservableProperty] private bool _isDateSelected;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdHeader))]
    [NotifyPropertyChangedFor(nameof(NameHeader))]
    [NotifyPropertyChangedFor(nameof(SourceHeader))]
    [NotifyPropertyChangedFor(nameof(DestinationHeader))]
    [NotifyPropertyChangedFor(nameof(TypeHeader))]
    private string sortColumn = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdHeader))]
    [NotifyPropertyChangedFor(nameof(NameHeader))]
    [NotifyPropertyChangedFor(nameof(SourceHeader))]
    [NotifyPropertyChangedFor(nameof(DestinationHeader))]
    [NotifyPropertyChangedFor(nameof(TypeHeader))]
    private bool sortAscending = true;

    public string IdHeader => "ID" + GetSortIndicator("Id");
    public string NameHeader => "Nom" + GetSortIndicator("Name");
    public string SourceHeader => "Source" + GetSortIndicator("Source");
    public string DestinationHeader => "Destination" + GetSortIndicator("Destination");
    public string TypeHeader => "Type" + GetSortIndicator("Type");

    public LogViewModel(BackupApplicationService backupAppService, ILocalizationService localizationService)
    {
        _backupService = backupAppService;
        _localizationService = localizationService;

        RefreshTranslations();
        LoadDates();
    }

        [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column)
            SortAscending = !SortAscending;
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
        Refresh();
    }

    public string GetSortIndicator(string column)
    {
        if (SortColumn != column) return "";
        return SortAscending ? " \u25b2" : " \u25bc";
    }

    /// <summary>
    /// Triggered when the log search text changes.
    /// </summary>
    /// <param name="value">The new search string.</param>
    partial void OnSearchTextChanged(string value) => Refresh();

    /// <summary>
    /// Triggered when the date search text changes.
    /// </summary>
    /// <param name="value">The new search string.</param>
    partial void OnSearchTextDateChanged(string value) => Refresh();

    /// <summary>
    /// Updates all localized strings based on the current language.
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
    }

    /// <summary>
    /// Command to select a specific date and display its associated logs.
    /// </summary>
    /// <param name="date">The date string selected by the user.</param>
    [RelayCommand]
    private void SelectDate(string date)
    {
        SelectedDate = date;
        IsDateSelected = true;
        LoadLogsForDate(date);
    }

    /// <summary>
    /// Command to go back to the date selection screen and reset search filters.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        IsDateSelected = false;
        SelectedDate = null;
        SearchText = string.Empty;
        _allLogEntries.Clear();
        Refresh();
    }

    /// <summary>
    /// Filters both dates and log entries based on their respective search queries.
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
        );

        foreach (var entry in filteredLogs)
            LogEntries.Add(entry);
    }

    /// <summary>
    /// Initializes the list of available dates (mock data).
    /// </summary>
    /// <summary>
    /// Fetches available log dates from the backend service and updates the list.
    /// </summary>
    private void LoadDates()
    {
        _allAvailableDates.Clear();

        var dates = _backupService.GetLogsDate();

        foreach (var date in dates)
        {
            _allAvailableDates.Add(date);
        }

        Refresh();
    }

    public void SetOnLanguageChanged(Action onLanguageChanged)
    {
        _onLanguageChanged = onLanguageChanged;
    }

    /// <summary>
    /// Loads log entries for a specific date by deserializing JSON data from the backend.
    /// </summary>
    /// <param name="date">The date of the logs to load.</param>
    private void LoadLogsForDate(string date)
    {
        _allLogEntries.Clear();

        var rawLogs = _backupService.GetLogsByDate(date);

        foreach (var log in rawLogs)
        {
            int typeValue = int.Parse(log.GetValueOrDefault("eventType")?.ToString() ?? "2");
            string typeName = Enum.GetName(typeof(LogEventType), typeValue) ?? "Error";

            long.TryParse(log.GetValueOrDefault("fileSizeBytes")?.ToString(), out long sizeInBytes);
            _allLogEntries.Add(new LogDisplayModel
            {
                Timestamp = DateTime.TryParse(log.GetValueOrDefault("timestamp")?.ToString(), out var dt)
                ? dt.ToString("HH:mm:ss")
                : "00:00:00",
                BackupName = log.GetValueOrDefault("backupName")?.ToString() ?? "",
                EventType = typeName,
                Source = log.GetValueOrDefault("sourcePathUNC")?.ToString() ?? "",
                Destination = log.GetValueOrDefault("destinationPathUNC")?.ToString() ?? "",
                FileSize = FormatFileSize(sizeInBytes),
                Duration = $"{log.GetValueOrDefault("transferTimeMs")} ms"
            });
        }

        Refresh();
    }

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