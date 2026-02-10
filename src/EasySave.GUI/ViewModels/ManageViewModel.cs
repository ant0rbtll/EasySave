using System.Collections.ObjectModel;
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

    [RelayCommand]
    private void RunJob(Models.BackupJob job)
    {
        //TODO ask confirmation via popup
        //_applicationService.RunJobById(job.Id);
        //job.IsActive = true;
    }

    [RelayCommand]
    private void ModifyJob(Models.BackupJob job)
    {
        // TODO: navigate to edit page
    }

    [RelayCommand]
    private void DeleteJob(Models.BackupJob job)
    {
        try
        {
            //TODO ask confirmation via popup
            //_applicationService.RemoveJob(job.Id);
            //_allJobs.Remove(job);
            //Refresh();
        }
        catch (Exception ex)
        {

        }

        // TODO: implement delete confirmation dialog
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
