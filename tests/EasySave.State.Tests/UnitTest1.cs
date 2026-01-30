using Xunit;
using System;
using System.Collections.Generic;
using EasySave.State;
using EasySave.Configuration.Paths;

public class RealTimeStateWriterTests
{
    [Fact]
    public void RealTimeState_Should_Write_File_Step_By_Step()
    {
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
            backupName = "TEST_BACKUP_REALTIME",
            progressPercent = 0
        };

        // STEP 1 ? Active
        entry.progressPercent = 25;
        writer.Update(entry);

        Console.WriteLine($"[UPDATE] {entry.backupName} ? {entry.status}");
        Console.WriteLine();

        // STEP 2 ? Done
        entry.progressPercent = 100;
        writer.Update(entry);

        Console.WriteLine($"[UPDATE] {entry.backupName} ? {entry.status}");
        Console.WriteLine();

        // STEP 3 ? Inactive
        writer.MarckInnactiv(1);

        Console.WriteLine($"[FINAL] {entry.backupName} ? {entry.status}");
        Console.WriteLine();

        // ASSERT FINAL
        Assert.Equal(BackupStatus.Inactive, state.Entries[1].status);
    }

}
