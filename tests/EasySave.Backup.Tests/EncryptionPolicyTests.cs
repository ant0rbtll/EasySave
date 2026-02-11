using EasySave.Backup;
using EasySave.Core;

namespace EasySave.Backup.Tests;

public class EncryptionPolicyTests
{
    [Fact]
    public void ShouldEncrypt_IsCaseInsensitiveAndNormalizesExtensions()
    {
        var policy = new EncryptionPolicy(["txt", ".LOG"], EncryptionProviderNames.DotNet, null);

        Assert.True(policy.ShouldEncrypt("/tmp/file.TXT"));
        Assert.True(policy.ShouldEncrypt("/tmp/error.log"));
        Assert.False(policy.ShouldEncrypt("/tmp/image.png"));
    }

    [Fact]
    public void ShouldEncrypt_WithoutConfiguredExtensions_ReturnsFalse()
    {
        var policy = new EncryptionPolicy([], EncryptionProviderNames.DotNet, null);

        Assert.False(policy.ShouldEncrypt("/tmp/file.txt"));
    }

    [Fact]
    public void ShouldEncrypt_NormalizesWhitespaceAndLeadingDot()
    {
        var policy = new EncryptionPolicy(["  txt  ", " log "], EncryptionProviderNames.DotNet, null);

        Assert.True(policy.ShouldEncrypt("/tmp/file.txt"));
        Assert.True(policy.ShouldEncrypt("/tmp/server.LOG"));
    }

    [Fact]
    public void ShouldEncrypt_WithoutFileExtension_ReturnsFalse()
    {
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null);

        Assert.False(policy.ShouldEncrypt("/tmp/readme"));
    }
}
