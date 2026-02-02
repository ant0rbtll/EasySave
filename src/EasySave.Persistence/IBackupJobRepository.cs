using EasySave.Core;

namespace EasySave.Persistence;

public interface IBackupJobRepository
{
    const int DefaultMaxJobs = 5;

    void Add(BackupJob job);
    void Remove(int id);
    BackupJob GetById(int id);
    List<BackupJob> GetAll();
    int Count();
    int MaxJobs();
}
