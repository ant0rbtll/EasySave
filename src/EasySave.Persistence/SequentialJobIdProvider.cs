using EasySave.Core;

namespace EasySave.Persistence;

/// <summary>
/// Fournisseur d'identifiants séquentiels basé sur l'ID maximum existant.
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

        var maxId = existing.Max(j => j.Id);
        if (maxId == int.MaxValue)
            throw new InvalidOperationException("Cannot generate a new job ID because the maximum allowed ID value has been reached.");
        return maxId + 1;
    }
}
