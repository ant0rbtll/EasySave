using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EasySave.Core;
using EasySave.Localization;
using static EasySave.GUI.Helpers.PathValidation;

namespace EasySave.GUI.ViewModels;

public partial class EditJobViewModel : ViewModelBase
{
    private const int NameMaxLength = 100;
    private readonly ILocalizationService _localizationService;

    public int JobId { get; }

    // Form fields
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string sourcePath = string.Empty;
    [ObservableProperty] private string destinationPath = string.Empty;
    [ObservableProperty] private BackupType selectedBackupType = BackupType.Complete;

    // Validation errors
    [ObservableProperty] private string? jobNameError;
    [ObservableProperty] private string? sourcePathError;
    [ObservableProperty] private string? destinationPathError;

    // Translated labels
    [ObservableProperty] private string titleText = string.Empty;
    [ObservableProperty] private string nameLabel = string.Empty;
    [ObservableProperty] private string sourceLabel = string.Empty;
    [ObservableProperty] private string destinationLabel = string.Empty;
    [ObservableProperty] private string backupTypeLabel = string.Empty;
    [ObservableProperty] private string browseButtonLabel = string.Empty;
    [ObservableProperty] private string browseSourceTitle = string.Empty;
    [ObservableProperty] private string browseDestinationTitle = string.Empty;
    [ObservableProperty] private string saveButtonLabel = string.Empty;
    [ObservableProperty] private string cancelButtonLabel = string.Empty;

    public ObservableCollection<BackupTypeOption> BackupTypeOptions { get; } = [];

    public EditJobViewModel(Models.BackupJob job, ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        JobId = job.Id;
        name = job.Name;
        sourcePath = job.Source;
        destinationPath = job.Destination;
        selectedBackupType = job.Type;
        LoadTranslations();
    }

    private void LoadTranslations()
    {
        TitleText = _localizationService.TranslateText(LocalizationKey.gui_manage_edit_title);
        NameLabel = _localizationService.TranslateText(LocalizationKey.gui_create_backup_name);
        SourceLabel = _localizationService.TranslateText(LocalizationKey.gui_create_source);
        DestinationLabel = _localizationService.TranslateText(LocalizationKey.gui_create_destination);
        BackupTypeLabel = _localizationService.TranslateText(LocalizationKey.gui_create_backup_type);
        BrowseButtonLabel = _localizationService.TranslateText(LocalizationKey.gui_create_browse);
        BrowseSourceTitle = _localizationService.TranslateText(LocalizationKey.gui_create_browse_source);
        BrowseDestinationTitle = _localizationService.TranslateText(LocalizationKey.gui_create_browse_destination);
        SaveButtonLabel = _localizationService.TranslateText(LocalizationKey.gui_manage_edit_save);
        CancelButtonLabel = _localizationService.TranslateText(LocalizationKey.gui_manage_btn_cancel);

        var current = SelectedBackupType;
        BackupTypeOptions.Clear();
        BackupTypeOptions.Add(new BackupTypeOption(BackupType.Complete,
            _localizationService.TranslateText(LocalizationKey.backupjob_type_complete)));
        BackupTypeOptions.Add(new BackupTypeOption(BackupType.Differential,
            _localizationService.TranslateText(LocalizationKey.backupjob_type_differential)));
        SelectedBackupType = current;
    }

    // Live-clearing of errors when user types valid values

    partial void OnNameChanged(string value)
    {
        if (JobNameError != null && !string.IsNullOrWhiteSpace(value) && value.Length <= NameMaxLength)
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

    // LostFocus validation

    public void ValidateNameOnLostFocus()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            JobNameError = _localizationService.TranslateText(LocalizationKey.gui_create_name_required);
        }
        else if (Name.Length > NameMaxLength)
        {
            JobNameError = string.Format(
                _localizationService.TranslateText(LocalizationKey.gui_create_name_too_long),
                NameMaxLength);
        }
        else
        {
            JobNameError = null;
        }
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

    public void ValidateAll()
    {
        ValidateNameOnLostFocus();
        ValidateSourcePathOnLostFocus();
        ValidateDestinationPathOnLostFocus();
    }

    public bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name)
            && Name.Length <= NameMaxLength
            && IsValidPath(SourcePath)
            && IsValidPath(DestinationPath)
            && JobNameError == null
            && SourcePathError == null
            && DestinationPathError == null;
    }

    // Browse dialog helpers

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

}
