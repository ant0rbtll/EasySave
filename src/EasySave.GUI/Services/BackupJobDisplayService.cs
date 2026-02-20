using System.Globalization;
using EasySave.Application;
using EasySave.Core;
using EasySave.Localization;

namespace EasySave.GUI.Services;

public class BackupJobDisplayService(
    BackupApplicationService applicationService,
    ILocalizationService localizationService) : IBackupJobDisplayService
{
    private readonly BackupApplicationService _applicationService = applicationService;
    private readonly ILocalizationService _localizationService = localizationService;

    public List<Models.BackupJob> FetchJobs()
    {
        try
        {
            return [.. _applicationService.GetAllJobs()
                .Select(job => new Models.BackupJob
                {
                    Id = job.Id,
                    Name = job.Name,
                    Source = job.Source,
                    Destination = job.Destination,
                    Type = job.Type,
                    LastExecutionDate = job.LastExecutionDate,
                    LastExecutionDisplay = FormatLastExecution(job.LastExecutionDate),
                    IsActive = job.IsActive,
                    Status = job.Status,
                    StatusDisplay = FormatStatus(job.Status)
                })];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public string FormatStatus(BackupJobStatus status)
    {
        return status switch
        {
            BackupJobStatus.Active => _localizationService.TranslateText(LocalizationKey.backupjob_active),
            BackupJobStatus.Paused => _localizationService.TranslateText(LocalizationKey.backupjob_paused),
            BackupJobStatus.Blocked => _localizationService.TranslateText(LocalizationKey.backupjob_blocked),
            BackupJobStatus.Waiting => _localizationService.TranslateText(LocalizationKey.backupjob_waiting),
            BackupJobStatus.Done => _localizationService.TranslateText(LocalizationKey.backupjob_done),
            BackupJobStatus.Error => _localizationService.TranslateText(LocalizationKey.backupjob_error),
            _ => _localizationService.TranslateText(LocalizationKey.backupjob_inactive)
        };
    }

    public string FormatLastExecution(DateTime? lastExecutionDate)
    {
        if (!lastExecutionDate.HasValue)
        {
            return _localizationService.TranslateText(LocalizationKey.backupjob_never);
        }

        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(_localizationService.Culture);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.CurrentCulture;
        }

        return lastExecutionDate.Value.ToString("g", culture);
    }

    public bool MatchesSearch(Models.BackupJob job, string query)
    {
        return job.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.StatusDisplay.Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.Source.Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.Destination.Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.Type.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.IsActive.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || job.LastExecutionDisplay.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    public List<Models.BackupJob> Sort(IEnumerable<Models.BackupJob> jobs, string sortColumn, bool ascending)
    {
        if (sortColumn == "LastRun")
        {
            return (ascending
                ? jobs
                    .OrderBy(j => j.LastExecutionDate.HasValue ? 0 : 1)
                    .ThenBy(j => j.LastExecutionDate)
                : jobs
                    .OrderBy(j => j.LastExecutionDate.HasValue ? 0 : 1)
                    .ThenByDescending(j => j.LastExecutionDate))
                .ToList();
        }

        Func<Models.BackupJob, object> keySelector = sortColumn switch
        {
            "Id" => j => j.Id,
            "Status" => j => j.Status,
            "Name" => j => j.Name,
            "Source" => j => j.Source,
            "Destination" => j => j.Destination,
            "Type" => j => j.Type,
            _ => j => j.Id
        };

        return [.. ascending
            ? jobs.OrderBy(keySelector)
            : jobs.OrderByDescending(keySelector)];
    }
}
