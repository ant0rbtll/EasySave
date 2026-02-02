using Xunit;
using EasySave.State;
using EasySave.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit.Abstractions;

public class RealTimeStateWriterTests
{
    [Fact]
    public void Update_WhenProgressLessThan100_SetsStatusActive()
    {
        // Utilise le chemin du TestPathProvider (chemin par défaut ou que tu veux)
        var pathProvider = new TestPathProvider(); // utilise DefaultStatePath
        var serializer = new StateSerializer();
        var state = new GlobalState
        {
            Entries = new Dictionary<int, StateEntry>()
        };

        var writer = new RealTimeStateWriter(
            pathProvider,
            serializer,
            state);

        var entry = new StateEntry
        {
            backupId = 1,
            status = BackupStatus.Active,
            backupName = "Test"
        };

        // Act
        writer.Update(entry);

        // Assert
        Assert.Equal(BackupStatus.Active, entry.status);
        Assert.True(state.Entries.ContainsKey(1));

        // ?? Affichage du chemin réel utilisé
        var filePath = pathProvider.GetStatePath();
        Console.WriteLine($"Chemin du fichier JSON : {filePath}");
        Console.WriteLine("Chemin absolu : " + Path.GetFullPath(filePath));


        // ?? Affichage du contenu réel du JSON
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            Console.WriteLine("JSON écrit :");
            Console.WriteLine(json);
        }
        else
        {
            Console.WriteLine("Le fichier n'existe pas !");
        }
    }

}

