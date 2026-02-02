using System.Runtime.InteropServices.Marshalling;
using EasySave.Persistence;
using EasySave.CLI;         
using EasySave.Backup;
using EasySave.UI;
using EasySave.Core;
namespace EasySave.Application;

public class BackupAppService
{
    private IBackupJobRepository Repo;
    private BackupEngine Engine;
    private IUI Ui;
    private CommandLineParser Parser;
    private List<BackupJob> Jobs;

    public BackupAppService(IBackupJobRepository repo, IUI ui)
    {
        Repo = repo;
        Engine = new BackupEngine();
        Ui = ui;
        Parser = new CommandLineParser();
    }

    public void CreateJob(string name,string source,string destination,BackupType type)
    {
        var job = new BackupJob
        {
            Name = name,
            Source = source,
            Destination = destination,
            Type = type
        };

        Repo.Add(job);
    }

    public void RemoveJob(int id)
    {
        Repo.Remove(id);
    }

    public void RunJobs(int[] ids)
    {
        foreach (int id in ids)
        {
            var job = Repo.GetById(id);
            if (job != null)
            {
                Engine.Execute(job);
            }
        }
    }

    public void RunAllJobs()
    {
        var jobs = Repo.GetAll();
        foreach (var job in jobs)
        {
            Engine.Execute(job);
        }
    }

    public List<BackupJob> GetAllJobs()
    {
        foreach (var job in Jobs)
        {
            Ui.ShowMessage($"[{job.Id}] {job.Name} | {job.Source} -> {job.Destination} ({job.Type})");
        }
        return Repo.GetAll();

        if (Jobs.Count == 0)
        {
            Ui.ShowMessage("Aucun travail de sauvegarde configuré.");
        }
        else
        {
            Ui.ShowMessage("Liste des travaux de sauvegarde :");
            foreach (var job in Jobs)
            {
                Ui.ShowMessage($"[{job.Id}] {job.name} | {job.source} -> {job.destination} ({job.type})");
            }
        }
    }

}
