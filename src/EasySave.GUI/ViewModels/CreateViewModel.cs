using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Core;
using EasySave.Application;
using EasySave.GUI.Helpers;
using EasySave.Localization;
using System.Collections.ObjectModel;

namespace EasySave.GUI.ViewModels;

public record BackupTypeOption(BackupType Value, string Label)
{
    public override string ToString() => Label;
}

public partial class CreateViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateJobCommand))]
    private string name = string.Empty;

    [ObservableProperty]
    private BackupType selectedBackupType = BackupType.Complete;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateJobCommand))]
    private string sourcePath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateJobCommand))]
    private string destinationPath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateJobCommand))]
    private string? sourcePathError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateJobCommand))]
    private string? destinationPathError;

    [ObservableProperty]
    private string? jobNameError;

    [ObservableProperty]
    private string? successMessage;

    [ObservableProperty]
    private bool isSuccessMessageVisible;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string subtitle = string.Empty;

    [ObservableProperty]
    private string backupNameLabel = string.Empty;

    [ObservableProperty]
    private string sourceLabel = string.Empty;

    [ObservableProperty]
    private string destinationLabel = string.Empty;

    [ObservableProperty]
    private string backupTypeLabel = string.Empty;

    [ObservableProperty]
    private string cancelButtonLabel = string.Empty;

    [ObservableProperty]
    private string createJobButtonLabel = string.Empty;

    [ObservableProperty]
    private string browseButtonLabel = string.Empty;

    private readonly BackupApplicationService _app;
    private readonly ILocalizationService _localizationService;
    private CancellationTokenSource? _dismissCts;

    public ObservableCollection<BackupTypeOption> BackupTypeOptions { get; } = [];

    public Action? OnJobCreated { get; set; }

    public CreateViewModel(
        BackupApplicationService app,
        ILocalizationService localizationService
        )
    {
        _app = app;
        _localizationService = localizationService;
        RefreshTranslations();
    }

    private bool CanCreateJob() =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(SourcePath) &&
        !string.IsNullOrWhiteSpace(DestinationPath) &&
        SourcePathError == null &&
        DestinationPathError == null;

    [RelayCommand(CanExecute = nameof(CanCreateJob))]
    private void CreateJob()
    {
        try
        {
            _app.CreateJob(Name, SourcePath, DestinationPath, SelectedBackupType);
            OnJobCreated?.Invoke();
            Cancel();
            ShowSuccessMessage(_localizationService.TranslateText(LocalizationKey.gui_create_add_success));
        }
        catch (ArgumentException ex)
        {
            JobNameError = ExceptionLocalizer.GetLocalizedMessage(ex, _localizationService);
        }
        catch (IOException ex)
        {
            DestinationPathError = ExceptionLocalizer.GetLocalizedMessage(ex, _localizationService);
        }
        catch (Exception ex)
        {
            JobNameError = ExceptionLocalizer.GetLocalizedMessage(ex, _localizationService);
        }
    }

    private void ShowSuccessMessage(string message)
    {
        SuccessMessage = message;
        IsSuccessMessageVisible = true;
        _ = AutoDismissSuccessAsync();
    }

    private async Task AutoDismissSuccessAsync()
    {
        var old = _dismissCts;
        old?.Cancel();
        old?.Dispose();
        var cts = _dismissCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(3000, cts.Token);
            IsSuccessMessageVisible = false;
            SuccessMessage = null;
        }
        catch (TaskCanceledException)
        {
            // A newer auto-dismiss replaced this one
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Name = string.Empty;
        SourcePath = string.Empty;
        DestinationPath = string.Empty;
        SelectedBackupType = BackupType.Complete;
        SourcePathError = null;
        DestinationPathError = null;
    }

    partial void OnNameChanged(string value)
    {
        if (JobNameError != null && !string.IsNullOrWhiteSpace(value) && value.Length <= 100)
            JobNameError = null;
    }

    partial void OnSourcePathChanged(string value)
    {
        if (SourcePathError != null && IsValidPath(value))
            SourcePathError = null;
    }

    partial void OnDestinationPathChanged(string value)
    {
        if (DestinationPathError != null && IsValidPath(value))
            DestinationPathError = null;
    }

    public void ValidateSourcePathOnLostFocus()
    {
        SourcePathError = IsValidPath(SourcePath)
            ? null
            : _localizationService.TranslateTextWithParams(
                LocalizationKey.gui_create_source_invalid, [ExamplePath("my_source")]);
    }

    public void ValidateDestinationPathOnLostFocus()
    {
        DestinationPathError = IsValidPath(DestinationPath)
            ? null
            : _localizationService.TranslateTextWithParams(
                LocalizationKey.gui_create_destination_invalid, [ExamplePath("my_destination")]);
    }

    private static string ExamplePath(string folder) =>
        OperatingSystem.IsWindows()
            ? $"C:\\Users\\user\\{folder}"
            : $"/home/user/{folder}";

    public void ValidateNameOnLostFocus()
    {
        const int inputMax = 100;

        if (string.IsNullOrWhiteSpace(Name))
        {
            JobNameError =
                _localizationService.TranslateText(
                    LocalizationKey.gui_create_name_required);
        }
        else if (Name.Length > inputMax)
        {
            JobNameError = string.Format(
                _localizationService.TranslateText(
                    LocalizationKey.gui_create_name_too_long),
                inputMax);
        }
        else
        {
            JobNameError = null;
        }
    }

    private static bool IsValidPath(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                path += Path.DirectorySeparatorChar;
            }
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return false;
            if (!Path.IsPathRooted(path)) return false;
            Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void SetSourcePathFromDialog(string path)
    {
        SourcePath = path;
        ValidateSourcePathOnLostFocus();
    }

    public void SetDestinationPathFromDialog(string path)
    {
        DestinationPath = path;
        ValidateDestinationPathOnLostFocus();
    }

    public void RefreshTranslations()
    {
        Title = _localizationService.TranslateText(LocalizationKey.gui_create_title);
        Subtitle = _localizationService.TranslateText(LocalizationKey.gui_create_subtitle);

        BackupNameLabel = _localizationService.TranslateText(LocalizationKey.gui_create_backup_name);
        SourceLabel = _localizationService.TranslateText(LocalizationKey.gui_create_source);
        DestinationLabel = _localizationService.TranslateText(LocalizationKey.gui_create_destination);
        BackupTypeLabel = _localizationService.TranslateText(LocalizationKey.gui_create_backup_type);

        BrowseButtonLabel = _localizationService.TranslateText(LocalizationKey.gui_create_browse);
        CreateJobButtonLabel = _localizationService.TranslateText(LocalizationKey.gui_create_create_job);
        CancelButtonLabel = _localizationService.TranslateText(LocalizationKey.gui_create_cancel);

        var current = SelectedBackupType;
        BackupTypeOptions.Clear();
        BackupTypeOptions.Add(new BackupTypeOption(BackupType.Complete,
            _localizationService.TranslateText(LocalizationKey.backupjob_type_complete)));
        BackupTypeOptions.Add(new BackupTypeOption(BackupType.Differential,
            _localizationService.TranslateText(LocalizationKey.backupjob_type_differential)));
        SelectedBackupType = current;
    }
}
