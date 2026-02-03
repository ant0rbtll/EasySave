using EasySave.Core;

namespace EasySave.Persistence;

/// <summary>
/// Fournisseur d'identifiants trouvant le plus petit ID disponible.
/// </summary>
public class SequentialJobIdProvider : IJobIdProvider
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Levée si la valeur maximale d'ID a été atteinte.
    /// </exception>
    public int NextId(List<BackupJob> existing)
    {
        if (existing == null || existing.Count == 0)
            return 1;

        // Trouve le plus petit ID disponible (trous dans la séquence)
        var existingIds = existing.Select(j => j.Id).OrderBy(id => id).ToList();

        int nextId = 1;
        foreach (var id in existingIds)
        {
            if (id == nextId)
            {
                nextId++;
            }
            else
            {
                // Trouvé un trou dans la séquence
                return nextId;
            }
        }

        // Pas de trou, retourne l'ID suivant après le max
        if (nextId == int.MaxValue)
            throw new InvalidOperationException("Cannot generate a new job ID because the maximum allowed ID value has been reached.");
        
        return nextId;
    }
}
