using EasySave.Exceptions;
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
            .Returns(new UserPreferences { BusinessSoftwareProcessNames = ["calc.exe"] });

        var guard = new BusinessSoftwareBackupExecutionGuard(
            preferencesRepositoryMock.Object,
            _ => true);

        var exception = Assert.Throws<EasysaveDefaultException>(() => guard.EnsureCanCopyNextFile());
        Assert.Equal(Localization.LocalizationKey.error_business_software_running, exception.ErrorKey);
        Assert.Equal("calc", exception.Options[0]);
    }

    [Fact]
    public void EnsureCanCopyNextFile_WhenNotConfigured_ShouldNotThrow()
    {
        var preferencesRepositoryMock = new Mock<IUserPreferencesRepository>();
        preferencesRepositoryMock
            .Setup(r => r.Load())
            .Returns(new UserPreferences { BusinessSoftwareProcessNames = [] });

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
            .Returns(new UserPreferences { BusinessSoftwareProcessNames = ["calc.exe", "excel"] });

        var guard = new BusinessSoftwareBackupExecutionGuard(
            preferencesRepositoryMock.Object,
            name => string.Equals(name, "excel", StringComparison.OrdinalIgnoreCase));

        var exception = Assert.Throws<EasysaveDefaultException>(() => guard.EnsureCanCopyNextFile());
        Assert.Equal(Localization.LocalizationKey.error_business_software_running, exception.ErrorKey);
        Assert.Equal("excel", exception.Options[0]);
    }
}
