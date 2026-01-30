namespace EasySave.State;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

public class StateSerializer
{
    public void WritePrettyJson(string path, GlobalState state)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Invalid state file path", nameof(path));

        if (state == null)
            throw new ArgumentNullException(nameof(state));

        // S'assure que le dossier existe
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Transforme le GlobalState en liste simplifiée
        var simplifiedEntries = state.Entries.Values.Select(entry => new
        {
            Name = entry.backupName,
            Status = entry.status.ToString(),
            Timestamp = entry.timestamp
        }).ToList();

        // Sérialisation JSON propre (liste simplifiée)
        var json = JsonSerializer.Serialize(
            simplifiedEntries,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        // Écriture dans le fichier
        File.WriteAllText(path, json);

        // Affichage console
        string jobState = json.Replace("{", "").Replace("}", "").Replace("[", "").Replace("]", "");

        Console.WriteLine("=========== STATE FILE UPDATED ===========");
        Console.WriteLine($"Path : {path}");
        Console.WriteLine(jobState);
        Console.WriteLine("==========================================");
        Console.WriteLine();
    }
}
