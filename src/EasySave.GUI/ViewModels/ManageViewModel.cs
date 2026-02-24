using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EasySave.Application;
using EasySave.Backup;
using EasySave.Application.Services;
using EasySave.GUI.Helpers;
using EasySave.GUI.Models;
using EasySave.GUI.Services;
using EasySave.Localization;
using EasySave.Exceptions;

namespace EasySave.GUI.ViewModels;

public partial class ManageViewModel : ViewModelBase
{
    public ObservableCollection<BackupJob> Jobs { get; } = [];
    public ObservableCollection<PaginationItem> PaginationItems { get; } = [];
    public IReadOnlyList<int> PageSizeOptions { get; } = [15, 25, 50];
    private readonly List<BackupJob> _allJobs = [];
    private readonly BackupApplicationService _applicationService;
    private readonly IBackupExecutionController _backupExecutionController;
    private readonly ILocalizationService _localizationService;
    private readonly IBackupJobDisplayService _displayService;
    private readonly IBackupRunningStateTracker _stateTracker;
    public int PageSize => Math.Max(1, SelectedPageSize);

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int currentPage = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyPropertyChangedFor(nameof(IsPaginationVisible))]
    [NotifyPropertyChangedFor(nameof(PageJumpWatermark))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int filteredJobsCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyPropertyChangedFor(nameof(IsPaginationVisible))]
    [NotifyPropertyChangedFor(nameof(PageJumpWatermark))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int selectedPageSize = 15;

    [ObservableProperty]
    private string pageInputText = string.Empty;

    public int TotalPages => PaginationHelper.CalculateTotalPages(FilteredJobsCount, PageSize);

    public string PageDisplay => $"{CurrentPage}/{TotalPages}";
    public string PageJumpWatermark => $"1-{TotalPages}";
    public bool IsPaginationVisible => TotalPages > 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdHeader))]
    [NotifyPropertyChangedFor(nameof(StatusHeader))]
    [NotifyPropertyChangedFor(nameof(NameHeader))]
    [NotifyPropertyChangedFor(nameof(SourceHeader))]
    [NotifyPropertyChangedFor(nameof(DestinationHeader))]
    [NotifyPropertyChangedFor(nameof(TypeHeader))]
    [NotifyPropertyChangedFor(nameof(LastRunHeader))]
    private string sortColumn = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdHeader))]
    [NotifyPropertyChangedFor(nameof(StatusHeader))]
    [NotifyPropertyChangedFor(nameof(NameHeader))]
    [NotifyPropertyChangedFor(nameof(SourceHeader))]
    [NotifyPropertyChangedFor(nameof(DestinationHeader))]
    [NotifyPropertyChangedFor(nameof(TypeHeader))]
    [NotifyPropertyChangedFor(nameof(LastRunHeader))]
    private bool sortAscending = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunJobCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ModifyJobCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteJobCommand))]
    [NotifyCanExecuteChangedFor(nameof(PauseRunningCommand))]
    [NotifyCanExecuteChangedFor(nameof(PlayRunningCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopRunningCommand))]
    private bool isRunning;

    [ObservableProperty]
    private string runningJobName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PauseRunningCommand))]
    [NotifyCanExecuteChangedFor(nameof(PlayRunningCommand))]
    private bool isPaused;

    [ObservableProperty]
    private bool isConfirmDialogOpen;

    [ObservableProperty]
    private BackupJob? pendingJob;

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
    private BackupJob? pendingDeleteJob;

    [ObservableProperty]
    private bool isEditDialogOpen;

    [ObservableProperty]
    private EditJobViewModel? editingJob;

    [ObservableProperty]
    private bool isConfirmRunSelectedDialogOpen;

    [ObservableProperty]
    private bool isConfirmDeleteSelectedDialogOpen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    private bool hasSelection;

    [ObservableProperty]
    private bool hasJobs;

    private bool _updatingSelection;

    private bool _isAllSelected;
    public bool IsAllSelected
    {
        get => _isAllSelected;
        set
        {
            if (_isAllSelected == value) return;
            _isAllSelected = value;
            OnPropertyChanged(nameof(IsAllSelected));

            _updatingSelection = true;
            foreach (var job in Jobs)
                job.IsSelected = value;
            _updatingSelection = false;

            UpdateHasSelection();
        }
    }

    public ManageViewModel(
        BackupApplicationService backupApplicationService,
        IBackupExecutionController backupExecutionController,
        ILocalizationService localizationService,
        IBackupJobDisplayService displayService,
        IBackupRunningStateTracker stateTracker)
    {
        _applicationService = backupApplicationService;
        _backupExecutionController = backupExecutionController;
        _localizationService = localizationService;
        _displayService = displayService;
        _stateTracker = stateTracker;
        RefreshTranslations();
    }

    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 1;
        Refresh();
    }

    partial void OnSelectedPageSizeChanged(int value)
    {
        if (value <= 0)
        {
            SelectedPageSize = 15;
            return;
        }

        CurrentPage = 1;
        Refresh();
    }

    public void OnJobSelectionChanged()
    {
        if (_updatingSelection) return;
        UpdateHasSelection();

        _updatingSelection = true;
        _isAllSelected = Jobs.Count > 0 && Jobs.All(j => j.IsSelected);
        OnPropertyChanged(nameof(IsAllSelected));
        _updatingSelection = false;
    }

    private void UpdateHasSelection()
    {
        HasSelection = _allJobs.Any(j => j.IsSelected);
    }

    private void Refresh()
    {
        Jobs.Clear();
        var query = SearchText.Trim();

        IEnumerable<BackupJob> filtered = _allJobs;

        if (!string.IsNullOrEmpty(query))
            filtered = filtered.Where(j => _displayService.MatchesSearch(j, query));

        var filteredJobs = !string.IsNullOrEmpty(SortColumn)
            ? _displayService.Sort(filtered, SortColumn, SortAscending)
            : filtered.ToList();
        FilteredJobsCount = filteredJobs.Count;

        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;
        else if (CurrentPage < 1)
            CurrentPage = 1;

        var skipCount = (CurrentPage - 1) * PageSize;
        foreach (var job in filteredJobs.Skip(skipCount).Take(PageSize))
            Jobs.Add(job);

        RebuildPaginationItems();

        _updatingSelection = true;
        _isAllSelected = Jobs.Count > 0 && Jobs.All(j => j.IsSelected);
        OnPropertyChanged(nameof(IsAllSelected));
        _updatingSelection = false;
    }
    private void ApplyJobs(List<Models.BackupJob> jobs)
    {
        _allJobs.Clear();
        _allJobs.AddRange(jobs);
        HasJobs = _allJobs.Count > 0;
        Refresh();
        RunJobCommand.NotifyCanExecuteChanged();
        RunSelectedCommand.NotifyCanExecuteChanged();
    }
    private void RebuildPaginationItems()
    {
        PaginationItems.Clear();
        foreach (var item in PaginationHelper.BuildVisibleItems(CurrentPage, TotalPages))
            PaginationItems.Add(item);
    }
}
