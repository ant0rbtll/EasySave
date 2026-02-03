using EasySave.Core;
using EasySave.Persistence;

namespace EasySave.Persistence.Tests;

public class SequentialJobIdProviderTests
{
    [Fact]
    public void NextId_EmptyList_Returns1()
    {
        var provider = new SequentialJobIdProvider();
        var result = provider.NextId(new List<BackupJob>());
        Assert.Equal(1, result);
    }

    [Fact]
    public void NextId_WithExisting_ReturnsMaxPlusOne()
    {
        var provider = new SequentialJobIdProvider();
        var jobs = new List<BackupJob>
        {
            new() { Id = 1 },
            new() { Id = 3 },
            new() { Id = 2 }
        };
        var result = provider.NextId(jobs);
        Assert.Equal(4, result);
    }
}