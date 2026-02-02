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

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="BackupAppService"/>.
    /// </summary>
    /// <param name="repo">Le dépôt utilisé pour la persistance des données.</param>
    /// <param name="ui">L'interface utilisateur pour l'affichage des messages.</param>
    public BackupAppService(IBackupJobRepository repo, IUI ui)
    {
        Repo = repo;
        Engine = new BackupEngine();
        Ui = ui;
        Parser = new CommandLineParser();
    }

    /// <summary>
    /// Crée et enregistre un nouveau travail de sauvegarde.
    /// </summary>
    /// <param name="name">Nom unique du travail.</param>
    /// <param name="source">Chemin du dossier source.</param>
    /// <param name="destination">Chemin du dossier de destination.</param>
    /// <param name="type">Type de sauvegarde (Complète ou Différentielle).</param>
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

    /// <summary>
    /// Supprime un travail de sauvegarde existant via son identifiant unique.
    /// </summary>
    /// <param name="id">Identifiant du travail à supprimer.</param>
    public void RemoveJob(int id)
    {
        Repo.Remove(id);
    }


    /// <summary>
    /// Exécute une liste spécifique de travaux de sauvegarde.
    /// </summary>
    /// <param name="ids">Tableau d'identifiants des travaux à lancer.</param>
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

    /// <summary>
    /// Récupère et exécute l'intégralité des travaux de sauvegarde enregistrés.
    /// </summary>
    public void RunAllJobs()
    {
        var jobs = Repo.GetAll();
        foreach (var job in jobs)
        {
            Engine.Execute(job);
        }
    }

    /// <summary>
    /// Affiche la liste des travaux de sauvegarde dans l'interface utilisateur 
    /// et retourne la liste complète depuis le dépôt.
    /// </summary>
    /// <returns>Une liste d'objets <see cref="BackupJob"/>.</returns>
    public List<BackupJob> GetAllJobs()
    {
        foreach (var job in Jobs)
        {
            Ui.ShowMessage($"[{job.Id}] {job.Name} | {job.Source} -> {job.Destination} ({job.Type})");
        }
        return Repo.GetAll();
    }

}
