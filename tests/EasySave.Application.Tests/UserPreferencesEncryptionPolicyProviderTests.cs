using EasySave.Backup;
using EasySave.Core;
using EasySave.Persistence;
using Moq;

namespace EasySave.Application.Tests;

public class UserPreferencesEncryptionPolicyProviderTests
{
    [Fact]
    public void GetPolicy_MapsPreferencesToEncryptionPolicy()
    {
        var repository = new Mock<IUserPreferencesRepository>();
        repository.Setup(r => r.Load()).Returns(new UserPreferences
        {
            EncryptionProvider = EncryptionProviderNames.External,
            CryptoSoftExecutablePath = "/opt/cryptosoft",
            EncryptedExtensions = ["txt", ".log"]
        });

        var provider = new UserPreferencesEncryptionPolicyProvider(repository.Object);

        var policy = provider.GetPolicy();

        Assert.Equal(EncryptionProviderNames.External, policy.ProviderName);
        Assert.Equal("/opt/cryptosoft", policy.CryptoSoftExecutablePath);
        Assert.True(policy.ShouldEncrypt("/tmp/a.TXT"));
        Assert.True(policy.ShouldEncrypt("/tmp/app.log"));
    }

    [Fact]
    public void GetPolicy_WhenRepositoryFails_ReturnsDisabledPolicy()
    {
        var repository = new Mock<IUserPreferencesRepository>();
        repository.Setup(r => r.Load()).Throws(new InvalidOperationException("boom"));

        var provider = new UserPreferencesEncryptionPolicyProvider(repository.Object);

        var policy = provider.GetPolicy();

        Assert.False(policy.ShouldEncrypt("/tmp/test.txt"));
    }
}
