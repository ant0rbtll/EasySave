using EasySave.Configuration;
using EasySave.State;
using Xunit;

namespace EasySave.State.Tests;

public class RealTimeStateWriterTests
{
    [Fact]
    public void Update_Should_Add_Entry_And_Write_State()
    {
        // Arrange
        var pathProvider = new FakePathProvider();
        var serializer = new FakeStateSerializer();
        var state = new GlobalState { Entries = new Dictionary<int, StateEntry>() };
        var writer = new RealTimeStateWriter(pathProvider, serializer, state);

        var entry = new StateEntry
        {
            backupId = 1,
            backupName = "TestBackup",
            status = BackupStatus.Active
        };

        // Act
        writer.Update(entry);

        // Console output
        Console.WriteLine($"Entry added: {state.Entries[1].backupName}, Status: {state.Entries[1].status}");
        Console.WriteLine($"Serializer called: {serializer.WasCalled}");

        // Assert
        Assert.True(state.Entries.ContainsKey(1));
        Assert.Equal(BackupStatus.Active, state.Entries[1].status);
    }

    [Fact]
    public void MarckInnactiv_Should_Set_State_To_Inactive()
    {
        // Arrange
        var pathProvider = new FakePathProvider();
        var serializer = new FakeStateSerializer();
        var state = new GlobalState
        {
            Entries = new Dictionary<int, StateEntry>()
        };

        state.Entries[1] = new StateEntry
        {
            backupId = 1,
            status = BackupStatus.Active
        };

        var writer = new RealTimeStateWriter(pathProvider, serializer, state);

        // Act
        writer.MarckInnactiv(1);

        // Assert
        Assert.Equal(BackupStatus.Inactive, state.Entries[1].status);
        Assert.True(serializer.WasCalled);
    }
}