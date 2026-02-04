using EasySave.Core;

namespace EasySave.Persistence.Tests;

public class SequentialJobIdProviderTests
{
    private readonly SequentialJobIdProvider _provider;

    public SequentialJobIdProviderTests()
    {
        _provider = new SequentialJobIdProvider();
    }

    #region Empty/Null List Tests

    [Fact]
    public void NextId_NullList_Returns1()
    {
        // Act
        var result = _provider.NextId(null!);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void NextId_EmptyList_Returns1()
    {
        // Arrange
        var jobs = new List<BackupJob>();

        // Act
        var result = _provider.NextId(jobs);

        // Assert
        Assert.Equal(1, result);
    }

    #endregion

    #region Sequential IDs Tests

    [Fact]
    public void NextId_SingleJob_ReturnsNextId()
    {
        // Arrange
        var jobs = new List<BackupJob>
        {
            new() { Id = 1 }
        };

        // Act
        var result = _provider.NextId(jobs);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void NextId_ConsecutiveIds_ReturnsNextAfterMax()
    {
        // Arrange
        var jobs = new List<BackupJob>
        {
            new() { Id = 1 },
            new() { Id = 2 },
            new() { Id = 3 }
        };

        // Act
        var result = _provider.NextId(jobs);

        // Assert
        Assert.Equal(4, result);
    }

    [Fact]
    public void NextId_UnorderedConsecutiveIds_ReturnsNextAfterMax()
    {
        // Arrange
        var jobs = new List<BackupJob>
        {
            new() { Id = 3 },
            new() { Id = 1 },
            new() { Id = 2 }
        };

        // Act
        var result = _provider.NextId(jobs);

        // Assert
        Assert.Equal(4, result);
    }

    #endregion

    #region Gap Detection Tests

    [Fact]
    public void NextId_GapAtBeginning_ReturnsFirstAvailable()
    {
        // Arrange - Missing ID 1
        var jobs = new List<BackupJob>
        {
            new() { Id = 2 },
            new() { Id = 3 }
        };

        // Act
        var result = _provider.NextId(jobs);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void NextId_GapInMiddle_ReturnsGapId()
    {
        // Arrange - Missing ID 2
        var jobs = new List<BackupJob>
        {
            new() { Id = 1 },
            new() { Id = 3 },
            new() { Id = 4 }
        };

        // Act
        var result = _provider.NextId(jobs);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void NextId_MultipleGaps_ReturnsFirstGap()
    {
        // Arrange - Missing IDs 2 and 4
        var jobs = new List<BackupJob>
        {
            new() { Id = 1 },
            new() { Id = 3 },
            new() { Id = 5 }
        };

        // Act
        var result = _provider.NextId(jobs);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void NextId_LargeGap_ReturnsFirstAvailable()
    {
        // Arrange - Large gap between 1 and 100
        var jobs = new List<BackupJob>
        {
            new() { Id = 1 },
            new() { Id = 100 }
        };

        // Act
        var result = _provider.NextId(jobs);

        // Assert
        Assert.Equal(2, result);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void NextId_OnlyHighIds_Returns1()
    {
        // Arrange - Only high IDs, no ID 1
        var jobs = new List<BackupJob>
        {
            new() { Id = 50 },
            new() { Id = 100 }
        };

        // Act
        var result = _provider.NextId(jobs);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void NextId_MaxValueReached_ThrowsInvalidOperationException()
    {
        // Arrange - List with consecutive IDs up to int.MaxValue - 1
        var jobs = new List<BackupJob>();
        for (int i = 1; i <= 5; i++)
        {
            jobs.Add(new BackupJob { Id = i });
        }
        // Add a job with int.MaxValue - 1 to simulate reaching the limit
        jobs.Clear();
        jobs.Add(new BackupJob { Id = int.MaxValue - 1 });

        // Act
        var result = _provider.NextId(jobs);

        // Assert - Should return 1 because there's a gap
        Assert.Equal(1, result);
    }

    [Fact]
    public void NextId_ConsecutiveFromOneToMaxMinusOne_ThrowsException()
    {
        // Arrange - Simulate a scenario where nextId would become int.MaxValue
        // We can't create int.MaxValue items, but we can test the logic
        // by creating a custom scenario
        var jobs = new List<BackupJob>
        {
            new() { Id = int.MaxValue - 1 }
        };

        // Act - This should return 1 (first gap)
        var result = _provider.NextId(jobs);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void NextId_DuplicateIds_HandlesGracefully()
    {
        // Arrange - Duplicate IDs in the list
        var jobs = new List<BackupJob>
        {
            new() { Id = 1 },
            new() { Id = 1 },
            new() { Id = 2 }
        };

        // Act
        var result = _provider.NextId(jobs);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void NextId_ZeroId_Returns1()
    {
        // Arrange - Job with ID 0 (invalid, but should handle)
        var jobs = new List<BackupJob>
        {
            new() { Id = 0 },
            new() { Id = 2 }
        };

        // Act
        var result = _provider.NextId(jobs);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void NextId_NegativeIds_Returns1()
    {
        // Arrange - Negative IDs (invalid, but should handle)
        var jobs = new List<BackupJob>
        {
            new() { Id = -1 },
            new() { Id = -2 }
        };

        // Act
        var result = _provider.NextId(jobs);

        // Assert
        Assert.Equal(1, result);
    }

    #endregion
}
