using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Application;
using EasySave.Core;

namespace EasySave.GUI.ViewModels;

public partial class ManageViewModel : ViewModelBase
{
    public ObservableCollection<Models.BackupJob> Jobs { get; } = new();
    private readonly List<Models.BackupJob> _allJobs = new();
    private readonly BackupApplicationService _applicationService;

    [ObservableProperty]
    private string searchText = string.Empty;

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
    private bool isStatusBannerVisible;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isStatusError;

    [ObservableProperty]
    private bool isDeleteDialogOpen;

    [ObservableProperty]
    private Models.BackupJob? pendingDeleteJob;

    public string IdHeader => "ID" + GetSortIndicator("Id");
    public string NameHeader => "Nom" + GetSortIndicator("Name");
    public string SourceHeader => "Source" + GetSortIndicator("Source");
    public string DestinationHeader => "Destination" + GetSortIndicator("Destination");
    public string TypeHeader => "Type" + GetSortIndicator("Type");

    public ManageViewModel(BackupApplicationService backupApplicationService)
    {
        _applicationService = backupApplicationService;
        LoadJobs();
    }

    partial void OnSearchTextChanged(string value)
    {
        Refresh();
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
            || job.Type.ToString().Contains(query, StringComparison.OrdinalIgnoreCase);
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

        try
        {
            await Task.Run(() => _applicationService.RunJobById(job.Id));
            success = true;
        }
        catch (Exception)
        {
            // BackupEngine already logs errors and updates state
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsRunning = false;
                RunningJobName = string.Empty;
                PendingJob = null;
                LoadJobs();

                if (success)
                {
                    StatusMessage = $"Job {job.Name} terminé avec succès";
                    IsStatusError = false;
                }
                else
                {
                    StatusMessage = $"Erreur lors de l'exécution du job {job.Name}";
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
        await Task.Delay(4000);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!IsStatusError)
                IsStatusBannerVisible = false;
        });
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
    private void ConfirmDelete()
    {
        if (PendingDeleteJob is not { } job)
            return;

        IsDeleteDialogOpen = false;
        IsStatusBannerVisible = false;

        bool success = false;

        try
        {
            _applicationService.RemoveJob(job.Id);
            success = true;
        }
        catch (Exception)
        {
            // Repository throws KeyNotFoundException if job not found
        }
        finally
        {
            PendingDeleteJob = null;
            LoadJobs();

            if (success)
            {
                StatusMessage = $"Job {job.Name} supprimé avec succès";
                IsStatusError = false;
            }
            else
            {
                StatusMessage = $"Erreur lors de la suppression du job {job.Name}";
                IsStatusError = true;
            }
            IsStatusBannerVisible = true;
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
        _allJobs.Clear();
        try
        {
            var coreJobs = _applicationService.GetAllJobs();
            foreach (var job in coreJobs)
            {
                _allJobs.Add(new Models.BackupJob
                {
                    Id = job.Id,
                    Name = job.Name,
                    Source = job.Source,
                    Destination = job.Destination,
                    Type = job.Type
                });
            }
        }
        catch (Exception)
        {
            // Repository may not be initialized yet
        }
        Refresh();
    }
}
