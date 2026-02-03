using EasySave.Core;

namespace EasySave.Persistence;

/// <summary>
/// Dépôt pour la gestion des jobs de sauvegarde.
/// </summary>
public interface IBackupJobRepository
{
    /// <summary>
    /// Nombre maximum de jobs autorisés par défaut.
    /// </summary>
    const int DefaultMaxJobs = 5;

    /// <summary>
    /// Ajoute un job au dépôt.
    /// </summary>
    /// <param name="job">Le job à ajouter.</param>
    void Add(BackupJob job);

    /// <summary>
    /// Supprime un job par son identifiant.
    /// </summary>
    /// <param name="id">L'identifiant du job à supprimer.</param>
    void Remove(int id);

    /// <summary>
    /// Récupère un job par son identifiant.
    /// </summary>
    /// <param name="id">L'identifiant du job.</param>
    /// <returns>Le job correspondant.</returns>
    BackupJob GetById(int id);

    /// <summary>
    /// Récupère tous les jobs.
    /// </summary>
    /// <returns>Liste de tous les jobs.</returns>
    List<BackupJob> GetAll();

    /// <summary>
    /// Retourne le nombre de jobs dans le dépôt.
    /// </summary>
    /// <returns>Le nombre de jobs.</returns>
    int Count();

    /// <summary>
    /// Retourne le nombre maximum de jobs autorisés.
    /// </summary>
    /// <returns>Le nombre maximum de jobs.</returns>
    int MaxJobs();
}
