using System.Globalization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using EasySave.Core;
using EasySave.GUI.Helpers;
using EasySave.Localization;

namespace EasySave.GUI.ViewModels;

public partial class ManageViewModel
{
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
        CurrentPage = 1;
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

    [RelayCommand(CanExecute = nameof(CanRunJob))]
    private void RunJob(Models.BackupJob job)
    {
        if (!CanRunJob(job))
            return;

        PendingJob = job;
        IsConfirmDialogOpen = true;
    }

    private bool CanRunJob(Models.BackupJob job) => _stateTracker.IsJobRunnable(job);

    [RelayCommand(CanExecute = nameof(CanPauseRunning))]
    private void PauseRunning()
    {
        if (_stateTracker.TryGetCurrentControlJobId(PendingJob?.Id, out var jobId))
            _backupExecutionController.Pause(jobId);
        else
            _backupExecutionController.PauseAll();

        RefreshPauseState();
    }

    private bool CanPauseRunning() => IsRunning && _stateTracker.CanPause(PendingJob?.Id);

    [RelayCommand(CanExecute = nameof(CanPlayRunning))]
    private void PlayRunning()
    {
        if (_stateTracker.TryGetCurrentControlJobId(PendingJob?.Id, out var jobId))
            _backupExecutionController.Resume(jobId);
        else
            _backupExecutionController.ResumeAll();

        RefreshPauseState();
    }

    private bool CanPlayRunning() => IsRunning && _stateTracker.CanPlay(PendingJob?.Id);

    [RelayCommand(CanExecute = nameof(CanStopRunning))]
    private void StopRunning()
    {
        if (_stateTracker.TryGetCurrentControlJobId(PendingJob?.Id, out var jobId))
            _backupExecutionController.RequestStop(jobId);
        else
            _backupExecutionController.RequestStopAll();

        RefreshPauseState();
    }

    private bool CanStopRunning() => IsRunning && _stateTracker.CanStop(PendingJob?.Id);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ConfirmRun()
    {
        if (PendingJob is not { } job)
            return;

        IsConfirmDialogOpen = false;
        _stateTracker.AddRunning(job.Id);
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
            if (ex.Data["errorKey"]?.ToString() == "error_job_not_found")
            {
                errorMessage = _localizationService.TranslateTextWithParams(
                    LocalizationKey.gui_manage_error_job_not_found_named,
                    [job.Name]);
            }
            else
            {
                stoppedByUser = IsStoppedByUser(ex);
                errorMessage = ExceptionLocalizer.GetLocalizedMessage(ex, _localizationService);
            }
        }
        finally
        {
            var jobs = await Task.Run(_displayService.FetchJobs);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsPaused = false;
                PendingJob = null;
                _stateTracker.RemoveRunning(job.Id);
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
            _stateTracker.AddRunning(j.Id);
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
            errorMessage = ExceptionLocalizer.GetLocalizedMessage(ex, _localizationService);
            success = false;
        }

        var jobs = await Task.Run(_displayService.FetchJobs);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var j in selectedJobs)
            {
                _stateTracker.RemoveRunning(j.Id);
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
                errorMessage = ExceptionLocalizer.GetLocalizedMessage(ex, _localizationService);
                success = false;
                break;
            }
        }

        var jobs = await Task.Run(_displayService.FetchJobs);

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

    private bool CanModifyJob(Models.BackupJob job) => !(IsRunning && (PendingJob?.Id == job.Id || _stateTracker.RunningJobIds.Contains(job.Id)));

    [RelayCommand(CanExecute = nameof(CanDeleteJob))]
    private void DeleteJob(Models.BackupJob job)
    {
        PendingDeleteJob = job;
        IsDeleteDialogOpen = true;
    }

    private bool CanDeleteJob(Models.BackupJob job) => !(IsRunning && (PendingJob?.Id == job.Id || _stateTracker.RunningJobIds.Contains(job.Id)));

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
            var jobs = await Task.Run(_displayService.FetchJobs);

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
            var errorMessage = ExceptionLocalizer.GetLocalizedMessage(ex, _localizationService);
            StatusMessage = errorMessage;
            IsStatusError = true;
            IsStatusBannerVisible = true;
            return;
        }

        IsEditDialogOpen = false;
        EditingJob = null;

        var jobs = await Task.Run(_displayService.FetchJobs);

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
        ApplyJobs(_displayService.FetchJobs());
    }

    private static bool IsStoppedByUser(Exception ex)
    {
        return string.Equals(ex.Data["errorKey"]?.ToString(), BackupRuntimeKeys.ErrorBackupStoppedByUser, StringComparison.Ordinal)
            || string.Equals(ex.Data["actionKey"]?.ToString(), BackupRuntimeKeys.ActionBackupStoppedByUser, StringComparison.Ordinal);
    }
}
