using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Core;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace EasySave.GUI.ViewModels;

public partial class CreateViewModel : ViewModelBase
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string sourcePath = string.Empty;

    [ObservableProperty]
    private string destinationPath = string.Empty;

    [ObservableProperty]
    private BackupType selectedBackupType = BackupType.Complete;

    public IReadOnlyList<BackupType> BackupTypes { get; } =
        new[] { BackupType.Complete, BackupType.Differential };

    // ?? Commandes publiques explicitement visibles
    public ICommand CreateJobCommand { get; }
    public ICommand CancelCommand { get; }

    public Action<BackupJob>? OnJobCreated { get; set; }

    public CreateViewModel()
    {
        CreateJobCommand = new RelayCommand(CreateJob, CanCreateJob);
        CancelCommand = new RelayCommand(ResetForm);
    }

    private bool CanCreateJob() =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(SourcePath) &&
        !string.IsNullOrWhiteSpace(DestinationPath);

    private void CreateJob()
    {
        var job = new BackupJob
        {
            Name = Name,
            Source = SourcePath,
            Destination = DestinationPath,
            Type = SelectedBackupType
        };

        OnJobCreated?.Invoke(job);
        ResetForm();
    }

    private void ResetForm()
    {
        Name = string.Empty;
        SourcePath = string.Empty;
        DestinationPath = string.Empty;
        SelectedBackupType = BackupType.Complete;
        ((RelayCommand)CreateJobCommand).NotifyCanExecuteChanged();
    }

    // Notification du CanExecute lorsque les propri�t�s changent
    partial void OnNameChanged(string value) => ((RelayCommand)CreateJobCommand).NotifyCanExecuteChanged();
    partial void OnSourcePathChanged(string value) => ((RelayCommand)CreateJobCommand).NotifyCanExecuteChanged();
    partial void OnDestinationPathChanged(string value) => ((RelayCommand)CreateJobCommand).NotifyCanExecuteChanged();

    public void SetSourcePath(string path) => SourcePath = path;
    public void SetDestinationPath(string path) => DestinationPath = path;
}
