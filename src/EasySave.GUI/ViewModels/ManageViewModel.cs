using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EasySave.Application;
using EasySave.Backup;
using EasySave.Core;
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

    private async Task RefreshRuntimeStatesOnceAsync(CancellationToken cancellationToken)
    {
        if (!await _runtimeRefreshGate.WaitAsync(0, cancellationToken))
            return;

        try
        {
            var runtimeStates = await Task.Run(
                () => _applicationService.GetAllJobsRuntimeStates(),
                cancellationToken);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ApplyRuntimeStates(runtimeStates);
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Polling stopped.
        }
        catch (Exception)
        {
            // Ignore transient state read issues during live refresh.
        }
        finally
        {
            _runtimeRefreshGate.Release();
        }
    }

    private void ApplyRuntimeStates(IReadOnlyDictionary<int, BackupJobRuntimeState> runtimeStates)
    {
        if (_allJobs.Count == 0)
            return;

        if (runtimeStates.Count != _allJobs.Count || _allJobs.Any(j => !runtimeStates.ContainsKey(j.Id)))
        {
            SyncRunningJobsFromRuntimeStates(runtimeStates);
            ApplyJobs(FetchJobs());
            return;
        }

        var runtimeChanged = false;
        foreach (var job in _allJobs)
        {
            var state = runtimeStates[job.Id];
            var statusDisplay = FormatStatus(state.Status);
            var lastExecutionDisplay = FormatLastExecution(state.LastExecutionDate);

            if (job.Status != state.Status)
            {
                job.Status = state.Status;
                runtimeChanged = true;
            }

            if (job.IsActive != state.IsActive)
            {
                job.IsActive = state.IsActive;
                runtimeChanged = true;
            }

            if (job.LastExecutionDate != state.LastExecutionDate)
            {
                job.LastExecutionDate = state.LastExecutionDate;
                runtimeChanged = true;
            }

            if (!string.Equals(job.StatusDisplay, statusDisplay, StringComparison.Ordinal))
            {
                job.StatusDisplay = statusDisplay;
                runtimeChanged = true;
            }

            if (!string.Equals(job.LastExecutionDisplay, lastExecutionDisplay, StringComparison.Ordinal))
            {
                job.LastExecutionDisplay = lastExecutionDisplay;
                runtimeChanged = true;
            }
        }

        runtimeChanged |= SyncRunningJobsFromRuntimeStates(runtimeStates);

        if (!runtimeChanged)
        {
            RefreshPauseStateFromController();
            return;
        }

        if (!string.IsNullOrWhiteSpace(SearchText) || SortColumn is "Status" or "LastRun")
        {
            Refresh();
        }

        RefreshPauseStateFromController();
    }

    private static bool MatchesSearch(Models.BackupJob job, string query)
    {
        return job.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.StatusDisplay.Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.Source.Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.Destination.Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.Type.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.IsActive.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.LastExecutionDisplay.Contains(query, StringComparison.OrdinalIgnoreCase);
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

    [RelayCommand(CanExecute = nameof(CanRunJob))]
    private void RunJob(Models.BackupJob job)
    {
        if (!CanRunJob(job))
            return;

        PendingJob = job;
        IsConfirmDialogOpen = true;
    }

    private bool CanRunJob(Models.BackupJob job) => IsJobRunnable(job);

    [RelayCommand(CanExecute = nameof(CanPauseRunning))]
    private void PauseRunning()
    {
        if (TryGetCurrentControlJobId(out var jobId))
            _backupExecutionController.Pause(jobId);
        else
            _backupExecutionController.PauseAll();

        RefreshPauseStateFromController();
    }

    private bool CanPauseRunning()
    {
        if (!IsRunning)
            return false;

        if (TryGetCurrentControlJobId(out var jobId))
            return TryGetRuntimeControlState(jobId, out var state) && state == BackupJobControlState.Running;

        return _runningJobIds.Any(id =>
            TryGetRuntimeControlState(id, out var state)
            && state == BackupJobControlState.Running);
    }

    [RelayCommand(CanExecute = nameof(CanPlayRunning))]
    private void PlayRunning()
    {
        if (TryGetCurrentControlJobId(out var jobId))
            _backupExecutionController.Resume(jobId);
        else
            _backupExecutionController.ResumeAll();

        RefreshPauseStateFromController();
    }

    private bool CanPlayRunning()
    {
        if (!IsRunning)
            return false;

        if (TryGetCurrentControlJobId(out var jobId))
            return TryGetRuntimeControlState(jobId, out var state) && state == BackupJobControlState.Paused;

        return _runningJobIds.Any(id =>
            TryGetRuntimeControlState(id, out var state)
            && state == BackupJobControlState.Paused);
    }

    [RelayCommand(CanExecute = nameof(CanStopRunning))]
    private void StopRunning()
    {
        if (TryGetCurrentControlJobId(out var jobId))
            _backupExecutionController.RequestStop(jobId);
        else
            _backupExecutionController.RequestStopAll();

        RefreshPauseStateFromController();
    }

    private bool CanStopRunning()
    {
        if (!IsRunning)
            return false;

        if (TryGetCurrentControlJobId(out var jobId))
            return TryGetRuntimeControlState(jobId, out var state) && state != BackupJobControlState.StopRequested;

        return _runningJobIds.Any(id =>
            TryGetRuntimeControlState(id, out var state)
            && state != BackupJobControlState.StopRequested);
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

    private void RefreshRunningState(string? runningLabel = null)
    {
        var runningCount = _runningJobIds.Count;
        IsRunning = runningCount > 0;

        if (runningCount == 0)
        {
            RunningJobName = string.Empty;
            IsPaused = false;
            return;
        }

        if (!string.IsNullOrWhiteSpace(runningLabel))
        {
            RunningJobName = runningLabel;
            RefreshPauseStateFromController();
            return;
        }

        if (runningCount == 1)
        {
            var singleJobId = _runningJobIds.First();
            RunningJobName = ResolveRunningJobLabel(singleJobId);
            RefreshPauseStateFromController();
            return;
        }

        if (runningCount > 1)
        {
            RunningJobName = runningCount.ToString(CultureInfo.InvariantCulture);
        }

        RefreshPauseStateFromController();
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ConfirmRun()
    {
        if (PendingJob is not { } job)
            return;

        IsConfirmDialogOpen = false;
        _runningJobIds.Add(job.Id);
        IsPaused = false;
        RunJobCommand.NotifyCanExecuteChanged();
        RunSelectedCommand.NotifyCanExecuteChanged();
        RefreshRunningState(job.Name);
        IsStatusBannerVisible = false;

        bool success = false;
        bool stoppedByUser = false;
        string errorMessage = string.Empty;

        try
        {
            await _applicationService.RunJob(job.Id);
            success = true;
        }
        catch (Exception ex)
        {
            if (ex is EasysaveDefaultException exception && exception.ErrorKey == LocalizationKey.error_job_not_found)
            {
                errorMessage = _localizationService.TranslateTextWithParams(
                    LocalizationKey.error_job_not_found_named,
                    [job.Name]);
            }
            else
            {
                stoppedByUser = IsStoppedByUser(ex);
                errorMessage = _localizationService.GetTranslateTextException(ex);
            }
        }
        finally
        {
            var jobs = await Task.Run(FetchJobs);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsPaused = false;
                PendingJob = null;
                _runningJobIds.Remove(job.Id);
                RefreshRunningState();
                ApplyJobs(jobs);
                RunJobCommand.NotifyCanExecuteChanged();
                RunSelectedCommand.NotifyCanExecuteChanged();

                if (success)
                {
                    StatusMessage = _localizationService.TranslateTextWithParams(LocalizationKey.gui_manage_run_success, new[] { job.Name });
                    IsStatusError = false;
                }
                else
                {
                    StatusMessage = errorMessage;
                    IsStatusError = !stoppedByUser;
                }
                IsStatusBannerVisible = true;
            });
        }

        if (success || stoppedByUser)
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

    [RelayCommand(CanExecute = nameof(CanRunSelected))]
    private void RunSelected()
    {
        var count = _allJobs.Count(j => j.IsSelected);
        if (count == 0)
            return;

        ConfirmRunSelectedMessage = _localizationService.TranslateTextWithParams(
            LocalizationKey.gui_manage_confirm_run_selected_message, [count.ToString()]);
        IsConfirmRunSelectedDialogOpen = true;
    }

    private bool CanRunSelected() => HasSelection && !IsRunning;

    [RelayCommand]
    private async Task ConfirmRunSelected()
    {
        var selectedJobs = _allJobs.Where(j => j.IsSelected).ToList();
        if (selectedJobs.Count == 0) return;

        IsConfirmRunSelectedDialogOpen = false;
        foreach (var j in selectedJobs)
            _runningJobIds.Add(j.Id);
        RunJobCommand.NotifyCanExecuteChanged();
        RunSelectedCommand.NotifyCanExecuteChanged();
        RefreshRunningState(selectedJobs.Count.ToString(CultureInfo.InvariantCulture));
        IsPaused = false;

        IsStatusBannerVisible = false;

        int total = selectedJobs.Count;
        bool success = true;
        bool stoppedByUser = false;
        string errorMessage = string.Empty;

        var selectedIds = selectedJobs.Select(j => j.Id).ToArray();

        try
        {
            await _applicationService.RunJobs(selectedIds);
        }
        catch (Exception ex)
        {
            stoppedByUser = IsStoppedByUser(ex);
            errorMessage = _localizationService.GetTranslateTextException(ex);
            success = false;
        }

        var jobs = await Task.Run(FetchJobs);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var j in selectedJobs)
            {
                _runningJobIds.Remove(j.Id);
            }
            IsPaused = false;
            RefreshRunningState();
            ApplyJobs(jobs);
            RunJobCommand.NotifyCanExecuteChanged();
            RunSelectedCommand.NotifyCanExecuteChanged();
            UpdateHasSelection();
            IsAllSelected = false;

            if (success)
            {
                StatusMessage = _localizationService.TranslateTextWithParams(
                    LocalizationKey.gui_manage_run_selected_success, [total.ToString()]);
                IsStatusError = false;
            }
            else
            {
                StatusMessage = errorMessage;
                IsStatusError = !stoppedByUser;
            }
            IsStatusBannerVisible = true;
        });

        if (success || stoppedByUser)
        {
            _ = AutoDismissBannerAsync();
        }
    }

    [RelayCommand]
    private void CancelRunSelected()
    {
        IsConfirmRunSelectedDialogOpen = false;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private void DeleteSelected()
    {
        var count = _allJobs.Count(j => j.IsSelected);
        ConfirmDeleteSelectedMessage = _localizationService.TranslateTextWithParams(
            LocalizationKey.gui_manage_confirm_delete_selected_message, [count.ToString()]);
        IsConfirmDeleteSelectedDialogOpen = true;
    }

    private bool CanDeleteSelected() => HasSelection && !IsRunning;

    [RelayCommand]
    private async Task ConfirmDeleteSelected()
    {
        var selectedJobs = _allJobs.Where(j => j.IsSelected).ToList();
        if (selectedJobs.Count == 0) return;

        IsConfirmDeleteSelectedDialogOpen = false;
        IsStatusBannerVisible = false;

        int total = selectedJobs.Count;
        bool success = true;
        string errorMessage = string.Empty;

        foreach (var job in selectedJobs)
        {
            try
            {
                await Task.Run(() => _applicationService.RemoveJob(job.Id));
            }
            catch (Exception ex)
            {
                errorMessage = _localizationService.GetTranslateTextException(ex);
                success = false;
                break;
            }
        }

        var jobs = await Task.Run(FetchJobs);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ApplyJobs(jobs);
            UpdateHasSelection();
            IsAllSelected = false;

            if (success)
            {
                StatusMessage = _localizationService.TranslateTextWithParams(
                    LocalizationKey.gui_manage_delete_selected_success, [total.ToString()]);
                IsStatusError = false;
            }
            else
            {
                StatusMessage = errorMessage;
                IsStatusError = true;
            }
            IsStatusBannerVisible = true;
        });

        if (success)
        {
            _ = AutoDismissBannerAsync();
        }
    }

    [RelayCommand]
    private void CancelDeleteSelected()
    {
        IsConfirmDeleteSelectedDialogOpen = false;
    }

    [RelayCommand(CanExecute = nameof(CanModifyJob))]
    private void ModifyJob(Models.BackupJob job)
    {
        EditingJob = new EditJobViewModel(job, _localizationService);
        IsEditDialogOpen = true;
    }

    private bool CanModifyJob(Models.BackupJob job) => !(IsRunning && (PendingJob?.Id == job.Id || _runningJobIds.Contains(job.Id)));

    [RelayCommand(CanExecute = nameof(CanDeleteJob))]
    private void DeleteJob(Models.BackupJob job)
    {
        PendingDeleteJob = job;
        IsDeleteDialogOpen = true;
    }

    private bool CanDeleteJob(Models.BackupJob job) => !(IsRunning && (PendingJob?.Id == job.Id || _runningJobIds.Contains(job.Id)));

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
            errorMessage = _localizationService.GetTranslateTextException(ex);
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
    private async Task ConfirmEdit()
    {
        if (EditingJob is null)
            return;

        EditingJob.ValidateAll();

        if (!EditingJob.CanSave())
            return;

        IsStatusBannerVisible = false;

        string jobName = EditingJob.Name;

        try
        {
            var domainJob = new Core.BackupJob
            {
                Id = EditingJob.JobId,
                Name = EditingJob.Name,
                Source = EditingJob.SourcePath,
                Destination = EditingJob.DestinationPath,
                Type = EditingJob.SelectedBackupType
            };
            await Task.Run(() => _applicationService.UpdateJob(domainJob));
        }
        catch (Exception ex)
        {
            var errorMessage = _localizationService.GetTranslateTextException(ex);
            StatusMessage = errorMessage;
            IsStatusError = true;
            IsStatusBannerVisible = true;
            return;
        }

        IsEditDialogOpen = false;
        EditingJob = null;

        var jobs = await Task.Run(FetchJobs);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ApplyJobs(jobs);
            StatusMessage = _localizationService.TranslateTextWithParams(
                LocalizationKey.gui_manage_edit_success, [jobName]);
            IsStatusError = false;
            IsStatusBannerVisible = true;
        });

        _ = AutoDismissBannerAsync();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditDialogOpen = false;
        EditingJob = null;
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
                    IsActive = job.IsActive,
                    Status = job.Status,
                    StatusDisplay = FormatStatus(job.Status)
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

    private string FormatStatus(Core.BackupJobStatus status)
    {
        return status switch
        {
            Core.BackupJobStatus.Active => _localizationService.TranslateText(LocalizationKey.backupjob_active),
            Core.BackupJobStatus.Paused => _localizationService.TranslateText(LocalizationKey.backupjob_paused),
            Core.BackupJobStatus.Blocked => _localizationService.TranslateText(LocalizationKey.backupjob_blocked),
            Core.BackupJobStatus.Waiting => _localizationService.TranslateText(LocalizationKey.backupjob_waiting),
            Core.BackupJobStatus.Done => _localizationService.TranslateText(LocalizationKey.backupjob_done),
            Core.BackupJobStatus.Error => _localizationService.TranslateText(LocalizationKey.backupjob_error),
            _ => _localizationService.TranslateText(LocalizationKey.backupjob_inactive)
        };
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
