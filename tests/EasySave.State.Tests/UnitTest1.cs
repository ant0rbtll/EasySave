using Xunit;
using System;
using System.IO;
using System.Collections.Generic;
using EasySave.State;
using EasySave.Configuration;
using System.Data;
using Xunit.Abstractions;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        Console.WriteLine("toto");
    }
}

/*public class RealTimeStateTests
{
    private readonly ITestOutputHelper _outputHelper;

    public RealTimeStateTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }
    [Fact]
    public void test()
    {
        _outputHelper.WriteLine("YOYO");
    }
    
    /*[Fact]
    public void RealTimeState_Should_Write_File_Step_By_Step()
    {
        Console.WriteLine("=== TEST START ===");

        var tempDir = Path.Combine(Path.GetTempPath(), "RealtimeStateTest");
        Directory.CreateDirectory(tempDir);

        var pathProvider = new DefaultPathProvider();
        var serializer = new StateSerializer();

        var state = new GlobalState
        {
            Entries = new Dictionary<int, StateEntry>()
        };

        var writer = new RealTimeStateWriter(pathProvider, serializer, state);

        var entry = new StateEntry
        {
            backupId = 1,
            backupName = "test",
            progressPercent = 0
        };

        writer.Update(entry);

        string statePath = pathProvider.GetStatePath();

        if (!File.Exists(statePath))
            throw new Exception("Chemin du fichier non créé ou non récupéré");

        Console.WriteLine("[OK] State file exists");

        string content = File.ReadAllText(statePath);

        if (string.IsNullOrWhiteSpace(content))
            throw new Exception("Fichier state.json vide");

        Console.WriteLine("[OK] State file contains data");

        Console.WriteLine("=== TEST PASSED ===");
    }
}*/


