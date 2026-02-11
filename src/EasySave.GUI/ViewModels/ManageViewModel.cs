using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Application;
using EasySave.GUI.Helpers;
using EasySave.Localization;

namespace EasySave.GUI.ViewModels;

public partial class ManageViewModel : ViewModelBase
{
    public ObservableCollection<Models.BackupJob> Jobs { get; } = [];
    private readonly List<Models.BackupJob> _allJobs = [];
    private readonly BackupApplicationService _applicationService;
    private readonly ILocalizationService _localizationService;
    private CancellationTokenSource? _dismissCts;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdHeader))]
    [NotifyPropertyChangedFor(nameof(NameHeader))]
    [NotifyPropertyChangedFor(nameof(SourceHeader))]
    [NotifyPropertyChangedFor(nameof(DestinationHeader))]
    [NotifyPropertyChangedFor(nameof(TypeHeader))]
    [NotifyPropertyChangedFor(nameof(LastRunHeader))]
    private string sortColumn = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdHeader))]
    [NotifyPropertyChangedFor(nameof(NameHeader))]
    [NotifyPropertyChangedFor(nameof(SourceHeader))]
    [NotifyPropertyChangedFor(nameof(DestinationHeader))]
    [NotifyPropertyChangedFor(nameof(TypeHeader))]
    [NotifyPropertyChangedFor(nameof(LastRunHeader))]
    private bool sortAscending = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunJobCommand))]
    private bool isRunning;

    [ObservableProperty]
    private string runningJobName = string.Empty;

    [ObservableProperty]
    private bool isConfirmDialogOpen;

    [ObservableProperty]
    private Models.BackupJob? pendingJob;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSuccessBannerVisible))]
    [NotifyPropertyChangedFor(nameof(IsErrorBannerVisible))]
    private bool isStatusBannerVisible;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSuccessBannerVisible))]
    [NotifyPropertyChangedFor(nameof(IsErrorBannerVisible))]
    private bool isStatusError;

    public bool IsSuccessBannerVisible => IsStatusBannerVisible && !IsStatusError;
    public bool IsErrorBannerVisible => IsStatusBannerVisible && IsStatusError;

    [ObservableProperty]
    private bool isDeleteDialogOpen;

    [ObservableProperty]
    private Models.BackupJob? pendingDeleteJob;

    // Localized text properties
    [ObservableProperty]
    private string titleText = string.Empty;

    [ObservableProperty]
    private string subtitleText = string.Empty;

    [ObservableProperty]
    private string searchWatermark = string.Empty;

    [ObservableProperty]
    private string actionsHeader = string.Empty;

    [ObservableProperty]
    private string runningLabel = string.Empty;

    [ObservableProperty]
    private string confirmRunTitle = string.Empty;

    [ObservableProperty]
    private string confirmRunMessage = string.Empty;

    [ObservableProperty]
    private string confirmDeleteTitle = string.Empty;

    [ObservableProperty]
    private string confirmDeleteMessage = string.Empty;

    [ObservableProperty]
    private string btnConfirmText = string.Empty;

    [ObservableProperty]
    private string btnCancelText = string.Empty;

    [ObservableProperty]
    private string btnDeleteText = string.Empty;

    [ObservableProperty]
    private string tooltipRun = string.Empty;

    [ObservableProperty]
    private string tooltipModify = string.Empty;

    [ObservableProperty]
    private string tooltipDelete = string.Empty;

    private string _idLabel = "ID";
    private string _nameLabel = string.Empty;
    private string _sourceLabel = string.Empty;
    private string _destinationLabel = string.Empty;
    private string _typeLabel = string.Empty;
    private string _lastRunLabel = string.Empty;

    public string IdHeader => _idLabel + GetSortIndicator("Id");
    public string NameHeader => _nameLabel + GetSortIndicator("Name");
    public string SourceHeader => _sourceLabel + GetSortIndicator("Source");
    public string DestinationHeader => _destinationLabel + GetSortIndicator("Destination");
    public string TypeHeader => _typeLabel + GetSortIndicator("Type");
    public string LastRunHeader => _lastRunLabel + GetSortIndicator("LastRun");

    public ManageViewModel(BackupApplicationService backupApplicationService, ILocalizationService localizationService)
    {
        _applicationService = backupApplicationService;
        _localizationService = localizationService;
        RefreshTranslations();
    }

    public void RefreshTranslations()
    {
        TitleText = _localizationService.TranslateText(LocalizationKey.gui_manage_title);
        SubtitleText = _localizationService.TranslateText(LocalizationKey.gui_manage_subtitle);
        SearchWatermark = _localizationService.TranslateText(LocalizationKey.gui_manage_search);
        ActionsHeader = _localizationService.TranslateText(LocalizationKey.gui_manage_actions);
        RunningLabel = _localizationService.TranslateText(LocalizationKey.gui_manage_running);
        ConfirmRunTitle = _localizationService.TranslateText(LocalizationKey.gui_manage_confirm_run_title);
        ConfirmRunMessage = _localizationService.TranslateText(LocalizationKey.gui_manage_confirm_run_message);
        ConfirmDeleteTitle = _localizationService.TranslateText(LocalizationKey.gui_manage_confirm_delete_title);
        ConfirmDeleteMessage = _localizationService.TranslateText(LocalizationKey.gui_manage_confirm_delete_message);
        BtnConfirmText = _localizationService.TranslateText(LocalizationKey.gui_manage_btn_confirm);
        BtnCancelText = _localizationService.TranslateText(LocalizationKey.gui_manage_btn_cancel);
        BtnDeleteText = _localizationService.TranslateText(LocalizationKey.gui_manage_btn_delete);
        TooltipRun = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_run);
        TooltipModify = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_modify);
        TooltipDelete = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_delete);

        _idLabel = _localizationService.TranslateText(LocalizationKey.backupjob_id);
        _nameLabel = _localizationService.TranslateText(LocalizationKey.backupjob_name);
        _sourceLabel = _localizationService.TranslateText(LocalizationKey.backupjob_source);
        _destinationLabel = _localizationService.TranslateText(LocalizationKey.backupjob_destination);
        _typeLabel = _localizationService.TranslateText(LocalizationKey.backupjob_type);
        _lastRunLabel = _localizationService.TranslateText(LocalizationKey.backupjob_last_executed);
        OnPropertyChanged(nameof(IdHeader));
        OnPropertyChanged(nameof(NameHeader));
        OnPropertyChanged(nameof(SourceHeader));
        OnPropertyChanged(nameof(DestinationHeader));
        OnPropertyChanged(nameof(TypeHeader));
        OnPropertyChanged(nameof(LastRunHeader));
        ApplyJobs(FetchJobs());
    }

    partial void OnSearchTextChanged(string value)
    {
        Refresh();
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
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

    private void Refresh()
    {
        Jobs.Clear();
        var query = SearchText.Trim();

        IEnumerable<Models.BackupJob> filtered = _allJobs;

        if (!string.IsNullOrEmpty(query))
            filtered = filtered.Where(j => MatchesSearch(j, query));

        if (!string.IsNullOrEmpty(SortColumn))
        {
            Func<Models.BackupJob, object> keySelector = SortColumn switch
            {
                "Id" => j => j.Id,
                "Name" => j => j.Name,
                "Source" => j => j.Source,
                "Destination" => j => j.Destination,
                "Type" => j => j.Type,
                "LastRun" => j => j.LastExecutionDate ?? DateTime.MinValue,
                _ => j => j.Id
            };

            filtered = SortAscending
                ? filtered.OrderBy(keySelector)
                : filtered.OrderByDescending(keySelector);
        }

        foreach (var job in filtered)
            Jobs.Add(job);
    }

    private static bool MatchesSearch(Models.BackupJob job, string query)
    {
        return job.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.Source.Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.Destination.Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.Type.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.IsActive.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.LastExecutionDisplay.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand(CanExecute = nameof(CanRunJob))]
    private void RunJob(Models.BackupJob job)
    {
        PendingJob = job;
        IsConfirmDialogOpen = true;
    }

    private bool CanRunJob(Models.BackupJob job) => !IsRunning;

    [RelayCommand]
    private async Task ConfirmRun()
    {
        if (PendingJob is not { } job)
            return;

        IsConfirmDialogOpen = false;
        IsRunning = true;
        RunningJobName = job.Name;
        IsStatusBannerVisible = false;

        bool success = false;
        string errorMessage = string.Empty;

        try
        {
            // Ensure the job still exists before attempting to run it so we don't report success for a no-op.
            var existingJob = await Task.Run(() => _applicationService.GetJob(job.Id));
            if (existingJob is null)
            {
                // Use a specific, localized error when the job cannot be found.
                errorMessage = _localizationService.TranslateText(LocalizationKey.error_job_not_found);
            }
            else
            {
                await Task.Run(() => _applicationService.RunJob(job.Id));
                success = true;
            }
        }
        catch (Exception ex)
        {
            errorMessage = ExceptionLocalizer.GetLocalizedMessage(ex, _localizationService);
        }
        finally
        {
            var jobs = await Task.Run(FetchJobs);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsRunning = false;
                RunningJobName = string.Empty;
                PendingJob = null;
                ApplyJobs(jobs);

                if (success)
                {
                    StatusMessage = _localizationService.TranslateTextWithParams(LocalizationKey.gui_manage_run_success, new[] { job.Name });
                    IsStatusError = false;
                }
                else
                {
                    StatusMessage = errorMessage;
                    IsStatusError = true;
                }
                IsStatusBannerVisible = true;
            });
        }

        if (success)
        {
            _ = AutoDismissBannerAsync();
        }
    }

    private async Task AutoDismissBannerAsync()
    {
        var old = _dismissCts;
        old?.Cancel();
        old?.Dispose();
        var cts = _dismissCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(4000, cts.Token);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!IsStatusError)
                    IsStatusBannerVisible = false;
            });
        }
        catch (TaskCanceledException)
        {
            // A newer auto-dismiss replaced this one
        }
    }

    [RelayCommand]
    private void DismissStatusBanner()
    {
        IsStatusBannerVisible = false;
    }

    [RelayCommand]
    private void CancelRun()
    {
        IsConfirmDialogOpen = false;
        PendingJob = null;
    }

    [RelayCommand]
    private void ModifyJob(Models.BackupJob job)
    {
        // TODO: navigate to edit page
    }

    [RelayCommand]
    private void DeleteJob(Models.BackupJob job)
    {
        PendingDeleteJob = job;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmDelete()
    {
        if (PendingDeleteJob is not { } job)
            return;

        IsDeleteDialogOpen = false;
        IsStatusBannerVisible = false;

        bool success = false;
        string errorMessage = string.Empty;

        try
        {
            await Task.Run(() => _applicationService.RemoveJob(job.Id));
            success = true;
        }
        catch (Exception ex)
        {
            errorMessage = ExceptionLocalizer.GetLocalizedMessage(ex, _localizationService);
        }
        finally
        {
            var jobs = await Task.Run(FetchJobs);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PendingDeleteJob = null;
                ApplyJobs(jobs);

                if (success)
                {
                    StatusMessage = _localizationService.TranslateTextWithParams(LocalizationKey.gui_manage_delete_success, new[] { job.Name });
                    IsStatusError = false;
                }
                else
                {
                    StatusMessage = errorMessage;
                    IsStatusError = true;
                }
                IsStatusBannerVisible = true;
            });
        }

        if (success)
        {
            _ = AutoDismissBannerAsync();
        }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        PendingDeleteJob = null;
    }

    [RelayCommand]
    private void LoadJobs()
    {
        ApplyJobs(FetchJobs());
    }

    private List<Models.BackupJob> FetchJobs()
    {
        try
        {
            return [.. _applicationService.GetAllJobs()
                .Select(job => new Models.BackupJob
                {
                    Id = job.Id,
                    Name = job.Name,
                    Source = job.Source,
                    Destination = job.Destination,
                    Type = job.Type,
                    LastExecutionDate = job.LastExecutionDate,
                    LastExecutionDisplay = FormatLastExecution(job.LastExecutionDate),
                    IsActive = job.IsActive
                })];
        }
        catch (Exception)
        {
            // Repository may not be initialized yet
            return [];
        }
    }

    private string FormatLastExecution(DateTime? lastExecutionDate)
    {
        if (!lastExecutionDate.HasValue)
        {
            return _localizationService.TranslateText(LocalizationKey.backupjob_never);
        }

        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(_localizationService.Culture);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.CurrentCulture;
        }

        return lastExecutionDate.Value.ToString("g", culture);
    }

    private void ApplyJobs(List<Models.BackupJob> jobs)
    {
        _allJobs.Clear();
        _allJobs.AddRange(jobs);
        Refresh();
    }
}
