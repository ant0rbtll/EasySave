namespace EasySave.System;

/// <summary>
/// Provides a guard invoked by the backup engine before each file transfer.
/// </summary>
public interface IBackupExecutionGuard
{
    /// <summary>
    /// Throws when backup execution must be interrupted.
    /// </summary>
    void EnsureCanCopyNextFile();
}

/// <summary>
/// Default guard that never blocks backup execution.
/// </summary>
public sealed class NoOpBackupExecutionGuard : IBackupExecutionGuard
{
    public void EnsureCanCopyNextFile()
    {
    }
}
