using System.Runtime.InteropServices.Marshalling;
using EasySave.Persistence;
using EasySave.CLI;         
using EasySave.Backup;
using EasySave.UI;
using EasySave.Core;
namespace EasySave.Application;

public class BackupAppService
{
    private ISaveWorkRepository repo;
    private BackupEngine engine;
    private IUI ui;
    private CommandLineParser parser;
    private List<SaveWork> jobs;

    public BackupAppService(ISaveWorkRepository repo, IUI ui)
    {
        this.repo = repo;
        this.engine = new BackupEngine();
        this.ui = ui;
        this.parser = new CommandLineParser();
    }

    public void runInteractive()
    {
        ui.showMenu();
    }

    public void runFromArgs(String[]  args)
    {
        int[] ids = parser.parse(args);
        runJobs(ids);
    }

    public void createJob(string name,string source,string destination,BackupType type)
    {
        var job = new SaveWork
        {
            name = name,
            source = source,
            destination = destination,
            type = type
        };

        repo.add(job);
    }

    public void removeJob(int id)
    {
        repo.remove(id);
    }

    public void runJobs(int[] ids)
    {
        foreach (int id in ids)
        {
            var job = repo.getById(id);
            if (job != null)
            {
                engine.execute(job);
            }
        }
    }

    public void runAllJobs()
    {
        var jobs = repo.getAll();
        foreach (var job in jobs)
        {
            engine.execute(job);
        }
    }

    public List<SaveWork> getAllJobs()
    {
        jobs = repo.getAll();

        if (jobs.Count == 0)
        {
            ui.DisplayMessage("Aucun travail de sauvegarde configuré.");
        }
        else
        {
            ui.DisplayMessage("Liste des travaux de sauvegarde :");
            foreach (var job in jobs)
            {
                ui.DisplayMessage($"[{job.Id}] {job.name} | {job.source} -> {job.destination} ({job.type})");
            }
        }
    }

}
