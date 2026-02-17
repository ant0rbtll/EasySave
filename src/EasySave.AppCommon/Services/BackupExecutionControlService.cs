using EasySave.Backup;

namespace EasySave.AppCommon.Services;

/// <summary>
/// Default implementation that delegates runtime controls to the backup execution controller.
/// </summary>
public sealed class BackupExecutionControlService(IBackupExecutionController executionController) : IBackupExecutionControlService
{
    private readonly IBackupExecutionController _executionController = executionController;

    public void Pause() => _executionController.Pause();

    public void Resume() => _executionController.Resume();

    public void Stop() => StopAll();

    public void StopAll() => _executionController.RequestStop();
}
