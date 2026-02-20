using EasySave.Core;

namespace EasySave.GUI.Services;

public interface IBackupJobDisplayService
{
    List<Models.BackupJob> FetchJobs();
    string FormatStatus(BackupJobStatus status);
    string FormatLastExecution(DateTime? lastExecutionDate);
    bool MatchesSearch(Models.BackupJob job, string query);
    List<Models.BackupJob> Sort(IEnumerable<Models.BackupJob> jobs, string sortColumn, bool ascending);
}
