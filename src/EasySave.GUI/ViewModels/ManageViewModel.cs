using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public string FilterByStatusLabel => _localizationService.TranslateText(LocalizationKey.gui_manage_filter_by_status);
    public string FilterByTypeLabel => _localizationService.TranslateText(LocalizationKey.gui_manage_filter_by_type);

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
    private bool _updatingFilters;

    public ObservableCollection<FilterItem<Core.BackupJobStatus>> StatusFilters { get; } = [];
    public ObservableCollection<FilterItem<Core.BackupType>> TypeFilters { get; } = [];
    private bool _isAllSelected;

    public bool IsStatusAllSelected => StatusFilters.Count == 0 || StatusFilters[0].IsSelected;
    public bool IsTypeAllSelected => TypeFilters.Count == 0 || TypeFilters[0].IsSelected;
    [RelayCommand]
    private void ResetFilters()
    {
        if (StatusFilters.Count > 0)
        {
            StatusFilters[0].IsSelected = true;
            for (int i = 1; i < StatusFilters.Count; i++)
                StatusFilters[i].IsSelected = false;
        }
        if (TypeFilters.Count > 0)
        {
            TypeFilters[0].IsSelected = true;
            for (int i = 1; i < TypeFilters.Count; i++)
                TypeFilters[i].IsSelected = false;
        }
        SearchText = string.Empty;
        CurrentPage = 1;
        Refresh();
    }
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

    private void InitializeFilters()
    {
        foreach (var f in StatusFilters) f.OnChanged = null;
        foreach (var f in TypeFilters) f.OnChanged = null;
        StatusFilters.Clear();
        TypeFilters.Clear();

        StatusFilters.Add(new FilterItem<Core.BackupJobStatus>
        {
            Value = null,
            Label = FilterAllStatusLabel,
            IsSelected = true,
            OnChanged = OnStatusFilterChanged
        });

        foreach (var status in Enum.GetValues<Core.BackupJobStatus>())
        {
            if (status == Core.BackupJobStatus.Done)
                continue;

            string label = status switch
            {
                Core.BackupJobStatus.Active => FilterStatusActive,
                Core.BackupJobStatus.Paused => FilterStatusPaused,
                Core.BackupJobStatus.Blocked => FilterStatusBlocked,
                Core.BackupJobStatus.Inactive => FilterStatusInactive,
                Core.BackupJobStatus.Error => FilterStatusError,
                Core.BackupJobStatus.Waiting => FilterStatusWaiting,
                _ => FilterStatusDefault
            };
            StatusFilters.Add(new FilterItem<Core.BackupJobStatus>
            {
                Value = status,
                Label = label,
                IsSelected = false,
                OnChanged = OnStatusFilterChanged
            });
        }

        TypeFilters.Add(new FilterItem<Core.BackupType>
        {
            Value = null,
            Label = _localizationService.TranslateText(LocalizationKey.gui_manage_filter_all_types),
            IsSelected = true,
            OnChanged = OnTypeFilterChanged
        });

        foreach (var type in Enum.GetValues<Core.BackupType>())
        {
            LocalizationKey key = type switch
            {
                Core.BackupType.Complete => LocalizationKey.backupjob_type_complete,
                Core.BackupType.Differential => LocalizationKey.backupjob_type_differential,
                _ => LocalizationKey.backupjob_type
            };
            TypeFilters.Add(new FilterItem<Core.BackupType>
            {
                Value = type,
                Label = _localizationService.TranslateText(key),
                IsSelected = false,
                OnChanged = OnTypeFilterChanged
            });
        }

        OnPropertyChanged(nameof(StatusFilters));
        OnPropertyChanged(nameof(TypeFilters));
    }

    private void OnStatusFilterChanged(FilterItem<Core.BackupJobStatus> changed)
    {
        if (_updatingFilters) return;
        if (StatusFilters.Count == 0) return;
        _updatingFilters = true;
        if (changed == StatusFilters[0] && changed.IsSelected)
        {
            for (int i = 1; i < StatusFilters.Count; i++)
                StatusFilters[i].IsSelected = false;
        }
        else if (changed != StatusFilters[0] && changed.IsSelected)
        {
            StatusFilters[0].IsSelected = false;
        }
        if (StatusFilters.Count > 1 && StatusFilters.Skip(1).All(f => !f.IsSelected))
            StatusFilters[0].IsSelected = true;
        _updatingFilters = false;
        OnPropertyChanged(nameof(IsStatusAllSelected));
        CurrentPage = 1;
        Refresh();
    }

    private void OnTypeFilterChanged(FilterItem<Core.BackupType> changed)
    {
        if (_updatingFilters) return;
        if (TypeFilters.Count == 0) return;
        _updatingFilters = true;
        if (changed == TypeFilters[0] && changed.IsSelected)
        {
            for (int i = 1; i < TypeFilters.Count; i++)
                TypeFilters[i].IsSelected = false;
        }
        else if (changed != TypeFilters[0] && changed.IsSelected)
        {
            TypeFilters[0].IsSelected = false;
        }
        if (TypeFilters.Count > 1 && TypeFilters.Skip(1).All(f => !f.IsSelected))
            TypeFilters[0].IsSelected = true;
        _updatingFilters = false;
        OnPropertyChanged(nameof(IsTypeAllSelected));
        CurrentPage = 1;
        Refresh();
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

        IEnumerable<Models.BackupJob> filtered = _allJobs;

        if (!string.IsNullOrEmpty(query))
            filtered = filtered.Where(j => _displayService.MatchesSearch(j, query));

        if (StatusFilters.Count != 0)
        {
            var selectedStatus = StatusFilters.Skip(1).Where(f => f.IsSelected).Select(f => f.Value!.Value).ToList();
            if (!StatusFilters[0].IsSelected && selectedStatus.Count > 0)
            {
                filtered = filtered.Where(j => selectedStatus.Contains(j.Status));
            }
        }

        if (TypeFilters.Count != 0)
        {
            var selectedTypes = TypeFilters.Skip(1).Where(f => f.IsSelected).Select(f => f.Value!.Value).ToList();
            if (!TypeFilters[0].IsSelected && selectedTypes.Count > 0)
            {
                filtered = filtered.Where(j => selectedTypes.Contains(j.Type));
            }
        }

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

    public partial class FilterItem<T> : ObservableObject
    where T : struct
    {
        public T? Value { get; init; }

        public string Label { get; init; } = string.Empty;

        public Action<FilterItem<T>>? OnChanged { get; set; }

        [ObservableProperty]
        private bool isSelected;

        partial void OnIsSelectedChanged(bool value)
        {
            OnChanged?.Invoke(this);
        }
    }
}

