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
    private bool _updatingFilters;
    private readonly HashSet<int> _runningJobIds = [];

    public ObservableCollection<FilterItem<Core.BackupJobStatus>> StatusFilters { get; } = [];
    public ObservableCollection<FilterItem<Core.BackupType>> TypeFilters { get; } = [];
    private bool _isAllSelected;

    public bool IsStatusAllSelected => StatusFilters.Count == 0 || StatusFilters[0].IsSelected;
    public bool IsTypeAllSelected => TypeFilters.Count == 0 || TypeFilters[0].IsSelected;

    // Remove single selected filter properties, use multi-select logic instead
    [RelayCommand]
    private void ResetFilters()
    {
        // Set only 'All' checked, others unchecked
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
        InitializeFilters();
    }

    public void StartLiveRefresh()
    {
        if (_liveRefreshCts is not null)
            return;

        _liveRefreshCts = new CancellationTokenSource();
        _liveRefreshTask = RunLiveRefreshLoopAsync(_liveRefreshCts.Token);
    }

    public void StopLiveRefresh()
    {
        var cts = _liveRefreshCts;
        if (cts is null)
            return;

        _liveRefreshCts = null;
        cts.Cancel();
        cts.Dispose();
        _liveRefreshTask = null;
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
        PlayText = _localizationService.TranslateText(LocalizationKey.gui_manage_play);
        PauseText = _localizationService.TranslateText(LocalizationKey.gui_manage_pause);
        StopText = _localizationService.TranslateText(LocalizationKey.gui_manage_stop);
        TooltipPlay = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_play);
        TooltipPause = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_pause);
        TooltipStop = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_stop);
        RunSelectedText = _localizationService.TranslateText(LocalizationKey.gui_manage_run_selected);
        DeleteSelectedText = _localizationService.TranslateText(LocalizationKey.gui_manage_delete_selected);
        SelectAllTooltip = _localizationService.TranslateText(LocalizationKey.gui_manage_select_all);
        EmptyTitleText = _localizationService.TranslateText(LocalizationKey.gui_manage_empty_title);
        EmptySubtitleText = _localizationService.TranslateText(LocalizationKey.gui_manage_empty_subtitle);

        _idLabel = _localizationService.TranslateText(LocalizationKey.backupjob_id);
        _statusLabel = _localizationService.TranslateText(LocalizationKey.backupjob_status);
        _nameLabel = _localizationService.TranslateText(LocalizationKey.backupjob_name);
        _sourceLabel = _localizationService.TranslateText(LocalizationKey.backupjob_source);
        _destinationLabel = _localizationService.TranslateText(LocalizationKey.backupjob_destination);
        _typeLabel = _localizationService.TranslateText(LocalizationKey.backupjob_type);
        _lastRunLabel = _localizationService.TranslateText(LocalizationKey.backupjob_last_executed);
        OnPropertyChanged(nameof(IdHeader));
        OnPropertyChanged(nameof(StatusHeader));
        OnPropertyChanged(nameof(NameHeader));
        OnPropertyChanged(nameof(SourceHeader));
        OnPropertyChanged(nameof(DestinationHeader));
        OnPropertyChanged(nameof(TypeHeader));
        OnPropertyChanged(nameof(LastRunHeader));
        ApplyJobs(FetchJobs());
    }
    private void InitializeFilters()
    {
        StatusFilters.Clear();
        TypeFilters.Clear();

        StatusFilters.Add(new FilterItem<Core.BackupJobStatus>
        {
            Value = null,
            Label = "All",
            IsSelected = true
        });

        foreach (var status in Enum.GetValues<Core.BackupJobStatus>())
        {
            StatusFilters.Add(new FilterItem<Core.BackupJobStatus>
            {
                Value = status,
                Label = FormatStatus(status),
                IsSelected = false
            });
        }

        TypeFilters.Add(new FilterItem<Core.BackupType>
        {
            Value = null,
            Label = "All",
            IsSelected = true
        });

        foreach (var type in Enum.GetValues<Core.BackupType>())
        {
            TypeFilters.Add(new FilterItem<Core.BackupType>
            {
                Value = type,
                Label = type.ToString(),
                IsSelected = false
            });
        }

        // Subscribe to IsSelected changes for filter logic
        foreach (var filter in StatusFilters)
        {
            filter.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FilterItem<Core.BackupJobStatus>.IsSelected))
                    OnStatusFilterChanged((FilterItem<Core.BackupJobStatus>)s!);
            };
        }
        foreach (var filter in TypeFilters)
        {
            filter.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FilterItem<Core.BackupType>.IsSelected))
                    OnTypeFilterChanged((FilterItem<Core.BackupType>)s!);
            };
        }
    }

    private void OnStatusFilterChanged(FilterItem<Core.BackupJobStatus> changed)
    {
        if (_updatingFilters) return;
        if (StatusFilters.Count == 0) return;
        _updatingFilters = true;
        if (changed == StatusFilters[0] && changed.IsSelected)
        {
            // If 'All' is checked, uncheck others
            for (int i = 1; i < StatusFilters.Count; i++)
                StatusFilters[i].IsSelected = false;
        }
        else if (changed != StatusFilters[0] && changed.IsSelected)
        {
            // If any other is checked, uncheck 'All'
            StatusFilters[0].IsSelected = false;
        }
        // If none are checked, check 'All'
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

        // Multi-select filter logic
        if (TypeFilters.Count != 0)
        {
            var selectedTypes = TypeFilters.Skip(1).Where(f => f.IsSelected).Select(f => f.Value!.Value).ToList();
            if (!TypeFilters[0].IsSelected && selectedTypes.Count > 0)
            {
                filtered = filtered.Where(j => selectedTypes.Contains(j.Type));
            }
        }
        if(StatusFilters.Count != 0)
        {
            var selectedStatus = StatusFilters.Skip(1).Where(f => f.IsSelected).Select(f => f.Value!.Value).ToList();
            if (!StatusFilters[0].IsSelected && selectedStatus.Count > 0)
            {
                filtered = filtered.Where(j => selectedStatus.Contains(j.Status));
            }

        }
        

        // Multi-select filter logic
        if (TypeFilters.Count != 0)
        {
            var selectedTypes = TypeFilters.Skip(1).Where(f => f.IsSelected).Select(f => f.Value!.Value).ToList();
            if (!TypeFilters[0].IsSelected && selectedTypes.Count > 0)
            {
                filtered = filtered.Where(j => selectedTypes.Contains(j.Type));
            }
        }
        if(StatusFilters.Count != 0)
        {
            var selectedStatus = StatusFilters.Skip(1).Where(f => f.IsSelected).Select(f => f.Value!.Value).ToList();
            if (!StatusFilters[0].IsSelected && selectedStatus.Count > 0)
            {
                filtered = filtered.Where(j => selectedStatus.Contains(j.Status));
            }

        }
        

        // Multi-select filter logic
        if (TypeFilters.Count != 0)
        {
            var selectedTypes = TypeFilters.Skip(1).Where(f => f.IsSelected).Select(f => f.Value!.Value).ToList();
            if (!TypeFilters[0].IsSelected && selectedTypes.Count > 0)
            {
                filtered = filtered.Where(j => selectedTypes.Contains(j.Type));
            }
        }
        if(StatusFilters.Count != 0)
        {
            var selectedStatus = StatusFilters.Skip(1).Where(f => f.IsSelected).Select(f => f.Value!.Value).ToList();
            if (!StatusFilters[0].IsSelected && selectedStatus.Count > 0)
            {
                filtered = filtered.Where(j => selectedStatus.Contains(j.Status));
            }

        }
        

        if (!string.IsNullOrEmpty(SortColumn))
        {
            if (SortColumn == "LastRun")
            {
                filtered = SortAscending
                    ? filtered
                        .OrderBy(j => j.LastExecutionDate.HasValue ? 0 : 1)
                        .ThenBy(j => j.LastExecutionDate)
                    : filtered
                        .OrderBy(j => j.LastExecutionDate.HasValue ? 0 : 1)
                        .ThenByDescending(j => j.LastExecutionDate);
            }
            else
            {
                Func<Models.BackupJob, object> keySelector = SortColumn switch
                {
                    "Id" => j => j.Id,
                    "Status" => j => j.Status,
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
        }

        var filteredJobs = filtered.ToList();
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

        // Standard 7-slot pagination:
        // Start:  1 2 3 4 5 ... N
        // Middle: 1 ... p-1 p p+1 ... N
        // End:    1 ... N-4 N-3 N-2 N-1 N
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

    private bool TryGetCurrentControlJobId(out int jobId)
    {
        if (_runningJobIds.Count == 1)
        {
            jobId = _runningJobIds.First();
            return true;
        }

        if (PendingJob is not null && _runningJobIds.Contains(PendingJob.Id))
        {
            jobId = PendingJob.Id;
            return true;
        }

        jobId = default;
        return false;
    }

    private void RefreshPauseStateFromController()
    {
        if (_runningJobIds.Count == 0)
        {
            IsPaused = false;
            PauseRunningCommand.NotifyCanExecuteChanged();
            PlayRunningCommand.NotifyCanExecuteChanged();
            StopRunningCommand.NotifyCanExecuteChanged();
            return;
        }

        if (TryGetCurrentControlJobId(out var jobId)
            && TryGetRuntimeControlState(jobId, out var controlState))
        {
            IsPaused = controlState == BackupJobControlState.Paused;
            PauseRunningCommand.NotifyCanExecuteChanged();
            PlayRunningCommand.NotifyCanExecuteChanged();
            StopRunningCommand.NotifyCanExecuteChanged();
            return;
        }

        var allPaused = _runningJobIds.Count > 0
            && _runningJobIds.All(id =>
                TryGetRuntimeControlState(id, out var state)
                && state == BackupJobControlState.Paused);
        IsPaused = allPaused;
        PauseRunningCommand.NotifyCanExecuteChanged();
        PlayRunningCommand.NotifyCanExecuteChanged();
        StopRunningCommand.NotifyCanExecuteChanged();
    }

    private bool SyncRunningJobsFromRuntimeStates(IReadOnlyDictionary<int, BackupJobRuntimeState> runtimeStates)
    {
        var activeJobIds = runtimeStates
            .Where(pair => pair.Value.IsActive)
            .Select(pair => pair.Key)
            .ToHashSet();

        if (_runningJobIds.SetEquals(activeJobIds))
            return false;

        _runningJobIds.Clear();
        foreach (var id in activeJobIds)
        {
            _runningJobIds.Add(id);
        }

        RefreshRunningState();
        RunJobCommand.NotifyCanExecuteChanged();
        RunSelectedCommand.NotifyCanExecuteChanged();
        return true;
    }

    private bool TryGetRuntimeControlState(int jobId, out BackupJobControlState controlState)
    {
        if (_backupExecutionController.TryGetCurrentJobControlState(jobId, out controlState))
            return true;

        if (_runningJobIds.Contains(jobId))
        {
            controlState = BackupJobControlState.Running;
            return true;
        }

        controlState = default;
        return false;
    }

    private string ResolveRunningJobLabel(int jobId)
    {
        var job = _allJobs.FirstOrDefault(j => j.Id == jobId);
        if (job is not null && !string.IsNullOrWhiteSpace(job.Name))
            return job.Name;

        return $"#{jobId}";
    }

    private bool IsJobRunnable(Models.BackupJob job)
    {
        return job.Status is not (Core.BackupJobStatus.Active or Core.BackupJobStatus.Waiting or Core.BackupJobStatus.Paused or Core.BackupJobStatus.Blocked)
            && !_runningJobIds.Contains(job.Id);
    }

    private static bool IsStoppedByUser(Exception ex)
    {
        return string.Equals(ex.Data["errorKey"]?.ToString(), BackupRuntimeKeys.ErrorBackupStoppedByUser, StringComparison.Ordinal)
            || string.Equals(ex.Data["actionKey"]?.ToString(), BackupRuntimeKeys.ActionBackupStoppedByUser, StringComparison.Ordinal);
    }
    public partial class FilterItem<T> : ObservableObject
    where T : struct
    {
        public T? Value { get; init; }

        public string Label { get; init; } = string.Empty;

        [ObservableProperty]
        private bool isSelected;
    }
}

public sealed class PaginationItem
{
    private PaginationItem()
    {
    }

    public int PageNumber { get; init; }
    public string Label { get; init; } = string.Empty;
    public bool IsEllipsis { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsPage => !IsEllipsis;
    public bool IsSelectable => !IsEllipsis && !IsCurrent;

    public static PaginationItem Page(int pageNumber, bool isCurrent)
    {
        return new PaginationItem
        {
            PageNumber = pageNumber,
            Label = pageNumber.ToString(CultureInfo.InvariantCulture),
            IsCurrent = isCurrent
        };
    }

    public static PaginationItem Ellipsis()
    {
        return new PaginationItem
        {
            Label = "...",
            IsEllipsis = true
        };
    }
}