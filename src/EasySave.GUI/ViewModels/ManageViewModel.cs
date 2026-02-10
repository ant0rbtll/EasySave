using EasySave.Application;
using EasySave.Core;
using EasySave.GUI.Models;

namespace EasySave.GUI.ViewModels;

public partial class ManageViewModel : ViewModelBase
{
    public List<Models.BackupJob> jobs { get; set; } = new List<Models.BackupJob>();
    private readonly BackupApplicationService _applicationService;

    public ManageViewModel(BackupApplicationService backupApplicationService)
    {
        _applicationService = backupApplicationService;
        getAllJobs(); 
    }
    public void getAllJobs() {
        try
        {
            //TODO link to app
            jobs = Convert(_applicationService.GetAllJobs());
        }
        catch (Exception ex)
        {

        }
    }

    public List<Models.BackupJob> Convert(List<Core.BackupJob> jobs)
    {
        return new List<Models.BackupJob>();
    }
}
