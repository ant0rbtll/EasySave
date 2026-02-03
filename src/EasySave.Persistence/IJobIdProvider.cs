using EasySave.Core;

namespace EasySave.Persistence;

/// <summary>
/// Fournit des identifiants uniques pour les jobs de sauvegarde.
/// </summary>
public interface IJobIdProvider
{
    /// <summary>
    /// Génère le prochain identifiant disponible.
    /// </summary>
    /// <param name="existing">Liste des jobs existants.</param>
    /// <returns>Le prochain identifiant unique.</returns>
    int NextId(List<BackupJob> existing);
}
