using EasySave.Persistence;
using Moq;

namespace EasySave.Application.Tests;

public class BusinessSoftwareBackupExecutionGuardTests
{
    [Fact]
    public void EnsureCanCopyNextFile_WhenBusinessSoftwareIsRunning_ShouldThrow()
    {
        var preferencesRepositoryMock = new Mock<IUserPreferencesRepository>();
        preferencesRepositoryMock
            .Setup(r => r.Load())
            .Returns(new UserPreferences { BusinessSoftwareProcessName = "calc.exe" });

        var guard = new BusinessSoftwareBackupExecutionGuard(
            preferencesRepositoryMock.Object,
            _ => true);

        var exception = Assert.Throws<InvalidOperationException>(() => guard.EnsureCanCopyNextFile());
        Assert.Equal("error_business_software_running", exception.Message);
        Assert.Equal("error_business_software_running", exception.Data["errorKey"]);
        Assert.Equal("calc", exception.Data["0"]);
    }

    [Fact]
    public void EnsureCanCopyNextFile_WhenNotConfigured_ShouldNotThrow()
    {
        var preferencesRepositoryMock = new Mock<IUserPreferencesRepository>();
        preferencesRepositoryMock
            .Setup(r => r.Load())
            .Returns(new UserPreferences { BusinessSoftwareProcessName = "   " });

        var guard = new BusinessSoftwareBackupExecutionGuard(
            preferencesRepositoryMock.Object,
            _ => true);

        var exception = Record.Exception(() => guard.EnsureCanCopyNextFile());
        Assert.Null(exception);
    }

    [Fact]
    public void EnsureCanCopyNextFile_WhenAnyConfiguredBusinessSoftwareIsRunning_ShouldThrow()
    {
        var preferencesRepositoryMock = new Mock<IUserPreferencesRepository>();
        preferencesRepositoryMock
            .Setup(r => r.Load())
            .Returns(new UserPreferences { BusinessSoftwareProcessName = "calc.exe, excel" });

        var guard = new BusinessSoftwareBackupExecutionGuard(
            preferencesRepositoryMock.Object,
            name => string.Equals(name, "excel", StringComparison.OrdinalIgnoreCase));

        var exception = Assert.Throws<InvalidOperationException>(() => guard.EnsureCanCopyNextFile());
        Assert.Equal("error_business_software_running", exception.Message);
        Assert.Equal("excel", exception.Data["0"]);
    }
}
