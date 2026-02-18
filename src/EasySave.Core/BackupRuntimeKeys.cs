namespace EasySave.Core;

/// <summary>
/// Centralized runtime keys shared across backup, application and UI layers.
/// </summary>
public static class BackupRuntimeKeys
{
    public const string ErrorBusinessSoftwareRunning = "error_business_software_running";
    public const string ErrorBackupStoppedByUser = "error_backup_stopped_by_user";
    public const string ActionBackupPausedByUser = "action_backup_paused_by_user";
    public const string ActionBackupStoppedByUser = "action_backup_stopped_by_user";
}
