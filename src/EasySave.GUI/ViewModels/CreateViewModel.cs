using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Core;
using EasySave.Application;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;

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
    #endregion

    private readonly BackupApplicationService _app;

    public IReadOnlyList<BackupType> BackupTypes { get; } =
        new[] { BackupType.Complete, BackupType.Differential };

    public ICommand CreateJobCommand { get; }
    public ICommand CancelCommand { get; }

    public Action<BackupJob>? OnJobCreated { get; set; }

    public CreateViewModel(BackupApplicationService app)
    {
        _app = app;

        CreateJobCommand = new RelayCommand(ExecuteCreateJob, CanExecuteCreateJob);
        CancelCommand = new RelayCommand(ExecuteCancel);
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
            ShowSuccessMessage("Travail de sauvegarde cr�� avec succ�s");
        }
        catch (ArgumentException ex)
        {
            JobNameError = ex.Message;
        }
        catch (IOException ex)
        {
            DestinationPathError = "Erreur d'acc�s au disque ou au dossier";
        }
        catch (Exception ex)
        {
            JobNameError = "Une erreur inattendue est survenue lors de la cr�ation du travail";
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
        SourcePathError = IsValidPath(SourcePath) ? null : "Chemin de source invalide, exemple : C:\\User\\exemple\\my_source";
        ((RelayCommand)CreateJobCommand).NotifyCanExecuteChanged();
    }

    public void ValidateDestinationPathOnLostFocus()
    {
        DestinationPathError = IsValidPath(DestinationPath) ? null : "Chemin de destination invalide, exemple : C:\\User\\exemple\\my_destination";
        ((RelayCommand)CreateJobCommand).NotifyCanExecuteChanged();
    }
    public void ValidateNameOnLostFocus()
    {
        int inputMax = 100;
        if (string.IsNullOrWhiteSpace(Name))
        {
            JobNameError = "Le nom est obligatoire";
        }
        else if (Name.Length > inputMax)
        {
            JobNameError = $"Nom de sauvegarde invalide, nombre de caract�res maximum : {inputMax}";
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
}
