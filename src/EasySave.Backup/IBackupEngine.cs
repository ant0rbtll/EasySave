using EasySave.Core;

namespace EasySave.Backup;

public interface IBackupEngine
{
    void Execute(BackupJob job);
}
