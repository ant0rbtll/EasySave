using EasySave.Core;

namespace EasySave.Persistence;

public interface IJobIdProvider
{
    int NextId(List<BackupJob> existing);
}
