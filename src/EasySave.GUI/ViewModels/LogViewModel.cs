using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.GUI.Models;
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
    private readonly List<string> _allAvailableDates = new();
    private readonly List<LogDisplayModel> _allLogEntries = new();

    public ObservableCollection<string> AvailableDates { get; } = new();
    public ObservableCollection<LogDisplayModel> LogEntries { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _searchTextDate = string.Empty;

    [ObservableProperty]
    private string? _selectedDate;

    [ObservableProperty]
    private bool _isDateSelected;

    public LogViewModel()
    {
        LoadDates();
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
    private void LoadDates()
    {
        _allAvailableDates.Clear();
        _allAvailableDates.Add("2026-02-10");
        _allAvailableDates.Add("2026-02-09");
        _allAvailableDates.Add("2026-02-08");

        Refresh();
    }

    /// <summary>
    /// Loads log entries for a specific date (mock data).
    /// </summary>
    /// <param name="date">The date for which logs should be loaded.</param>
    private void LoadLogsForDate(string date)
    {
        _allLogEntries.Clear();

        _allLogEntries.Add(new LogDisplayModel
        {
            Timestamp = "14:20:05",
            BackupName = "Work_Files",
            EventType = LogEventType.TransferFile.ToString(),
            Source = "C:/Docs/budget.xlsx",
            Destination = "D:/Backup/budget.xlsx",
            FileSize = "45 KB",
            Duration = "12 ms"
        });

        _allLogEntries.Add(new LogDisplayModel
        {
            Timestamp = "14:20:06",
            BackupName = "Work_Files",
            EventType = LogEventType.CreateDirectory.ToString(),
            Source = "C:/Docs/Invoices",
            Destination = "D:/Backup/Invoices",
            FileSize = "0 B",
            Duration = "2 ms"
        });

        _allLogEntries.Add(new LogDisplayModel
        {
            Timestamp = "14:21:10",
            BackupName = "Work_Files",
            EventType = LogEventType.Error.ToString(),
            Source = "C:/Docs/locked_file.dat",
            Destination = "D:/Backup/locked_file.dat",
            FileSize = "0 B",
            Duration = "0 ms"
        });

        Refresh();
    }
}