using EasySave.Core;
using EasySave.System;
using Moq;

namespace EasySave.Backup.Tests;

public class DefaultBackupFilePlannerTests
{
    [Fact]
    public void BuildPlans_OrdersPriorityExtensionsFirst_AndNormalizesExtensions()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(["/source/report.txt", "/source/image.png"]);
        fileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        fileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
        fileSystem.Setup(fs => fs.GetFileSize(It.IsAny<string>())).Returns(10);

        var planner = new DefaultBackupFilePlanner(fileSystem.Object);
        var job = new BackupJob
        {
            Source = "/source",
            Destination = "/dest",
            Type = BackupType.Complete
        };

        var plans = planner.BuildPlans(job, ["txt"]);

        Assert.Equal(2, plans.Count);
        Assert.True(plans[0].IsPriority);
        Assert.Equal("/source/report.txt", plans[0].SourceFile);
        Assert.True(plans[0].ShouldCopy);
        Assert.False(plans[1].IsPriority);
    }

    [Fact]
    public void BuildPlans_Differential_SetsShouldCopyOnlyForNewOrModifiedFiles()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(["/source/new.txt", "/source/unchanged.txt"]);
        fileSystem.Setup(fs => fs.DirectoryExists("/dest")).Returns(true);
        fileSystem.Setup(fs => fs.FileExists("/dest/new.txt")).Returns(false);
        fileSystem.Setup(fs => fs.FileExists("/dest/unchanged.txt")).Returns(true);
        fileSystem.Setup(fs => fs.GetFileSize("/source/new.txt")).Returns(10);
        fileSystem.Setup(fs => fs.GetFileSize("/source/unchanged.txt")).Returns(10);
        fileSystem.Setup(fs => fs.GetFileSize("/dest/unchanged.txt")).Returns(10);

        var planner = new DefaultBackupFilePlanner(fileSystem.Object);
        var job = new BackupJob
        {
            Source = "/source",
            Destination = "/dest",
            Type = BackupType.Differential
        };

        var plans = planner.BuildPlans(job, [".txt"]);

        Assert.Equal(2, plans.Count);
        Assert.True(plans[0].ShouldCopy);
        Assert.False(plans[1].ShouldCopy);
    }
}
