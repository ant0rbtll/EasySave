using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Core;
using EasySave.Application;
using System.Windows.Input;
using EasySave.Localization;

namespace EasySave.GUI.ViewModels;

public partial class CreateViewModel : ViewModelBase
{

    public event Action? SourcePathSelected;
    public event Action? DestinationPathSelected;


    /// <summary>
    /// ObservableProperty of CreateViewModel
    /// </summary>
    #region ObservableProperty
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private BackupType selectedBackupType = BackupType.Complete;

    [ObservableProperty]
    private string sourcePath = string.Empty;

    [ObservableProperty]
    private string destinationPath = string.Empty;

    [ObservableProperty]
    private string? sourcePathError;

    [ObservableProperty]
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
    private string browseSourceButtonLabel = string.Empty;
    #endregion

    private readonly BackupApplicationService _app;
    private readonly ILocalizationService _localizationService;

    public IReadOnlyList<BackupType> BackupTypes { get; } =
        new[] { BackupType.Complete, BackupType.Differential };

    public ICommand CreateJobCommand { get; }
    public ICommand CancelCommand { get; }

    public Action<BackupJob>? OnJobCreated { get; set; }

    public CreateViewModel(
        BackupApplicationService app,
        ILocalizationService localizationService
        )
    {
        _app = app;
        _localizationService = localizationService;
        CreateJobCommand = new RelayCommand(ExecuteCreateJob, CanExecuteCreateJob);
        CancelCommand = new RelayCommand(ExecuteCancel);

        RefreshTranslations();
    }

    private bool CanExecuteCreateJob() =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(SourcePath) &&
        !string.IsNullOrWhiteSpace(DestinationPath) &&
        SourcePathError == null &&
        DestinationPathError == null;

    /// <summary>
    /// Send data 
    /// </summary>
    #region ExecuteCreateJob
    private void ExecuteCreateJob()
    {
        try
        {
            var job = new BackupJob
            {
                Name = Name,
                Source = SourcePath,
                Destination = DestinationPath,
                Type = SelectedBackupType
            };
            _app.CreateJob(
                job.Name,
                job.Source,
                job.Destination,
                job.Type);

            OnJobCreated?.Invoke(job);
            ExecuteCancel();
            ShowSuccessMessage(_localizationService.TranslateText(LocalizationKey.gui_create_add_success));
        }
        catch (ArgumentException ex)
        {
            JobNameError = ex.Message;
        }
        catch (IOException)
        {
            DestinationPathError = _localizationService.TranslateText(LocalizationKey.gui_create_error_io);
        }
        catch (Exception)
        {
            JobNameError = _localizationService.TranslateText(LocalizationKey.gui_create_error_unexpected);
        }
    }
    private async void ShowSuccessMessage(string message)
    {
        SuccessMessage = message;
        IsSuccessMessageVisible = true;

        await Task.Delay(3000); // 3 secondes

        IsSuccessMessageVisible = false;
        SuccessMessage = null;
    }
    #endregion

    /// <summary>
    /// Cleaning of input areas 
    /// </summary>
    #region ExecuteCancel
    private void ExecuteCancel()
    {
        Name = string.Empty;
        SourcePath = string.Empty;
        DestinationPath = string.Empty;
        SelectedBackupType = BackupType.Complete;
        SourcePathError = null;
        DestinationPathError = null;

        ((RelayCommand)CreateJobCommand).NotifyCanExecuteChanged();
    }
    #endregion

    /// <summary>
    /// Input validation 
    /// </summary>
    #region ValidatePath
    public void ValidateSourcePathOnLostFocus()
    {
        SourcePathError = IsValidPath(SourcePath)
            ? null
            : _localizationService.TranslateText(
                LocalizationKey.gui_create_source_invalid);

        ((RelayCommand)CreateJobCommand).NotifyCanExecuteChanged();
    }
    public void ValidateDestinationPathOnLostFocus()
    {
        DestinationPathError = IsValidPath(DestinationPath)
            ? null
            : _localizationService.TranslateText(
                LocalizationKey.gui_create_destination_invalid);

        ((RelayCommand)CreateJobCommand).NotifyCanExecuteChanged();
    }
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

    ((RelayCommand)CreateJobCommand).NotifyCanExecuteChanged();
    }
    private static bool IsValidPath(string path)
    {
        try
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                path += Path.DirectorySeparatorChar;
            }
            if (string.IsNullOrWhiteSpace(path)) return false;
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
    public void SetSourcePath(string path) => SourcePath = path; 
    public void SetDestinationPath(string path) => DestinationPath = path;

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
    #endregion
    /// <summary>
    /// Translations
    /// </summary>
    #region RefreshTranslations
    public void RefreshTranslations()
    {
        Title = _localizationService.TranslateText(LocalizationKey.gui_create_title);
        Subtitle = _localizationService.TranslateText(LocalizationKey.gui_create_subtitle);

        BackupNameLabel = _localizationService.TranslateText(LocalizationKey.gui_create_backup_name);
        SourceLabel = _localizationService.TranslateText(LocalizationKey.gui_create_source);
        DestinationLabel = _localizationService.TranslateText(LocalizationKey.gui_create_destination);
        BackupTypeLabel = _localizationService.TranslateText(LocalizationKey.gui_create_backup_type);

        BrowseSourceButtonLabel = _localizationService.TranslateText(LocalizationKey.gui_create_browse);
        CreateJobButtonLabel = _localizationService.TranslateText(LocalizationKey.gui_create_create_job);
        CancelButtonLabel = _localizationService.TranslateText(LocalizationKey.gui_create_cancel);
    }
    #endregion
}
