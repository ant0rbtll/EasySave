using EasySave.Core;

namespace EasySave.Backup.Tests;

public sealed class EncryptionProviderResolverTests
{
    [Fact]
    public void Resolve_WhenProviderExists_ReturnsMatchingProvider()
    {
        var dotNetProvider = new StubProvider(EncryptionProviderNames.DotNet);
        var externalProvider = new StubProvider(EncryptionProviderNames.External);
        var resolver = new EncryptionProviderResolver([dotNetProvider, externalProvider]);

        var resolved = resolver.Resolve("dotnet");

        Assert.Same(dotNetProvider, resolved);
    }

    [Fact]
    public void Resolve_WhenProviderDoesNotExist_ReturnsNull()
    {
        var resolver = new EncryptionProviderResolver([new StubProvider(EncryptionProviderNames.DotNet)]);

        var resolved = resolver.Resolve("missing-provider");

        Assert.Null(resolved);
    }

    [Fact]
    public void Resolve_WhenNameIsNullOrWhitespace_ReturnsNull()
    {
        var resolver = new EncryptionProviderResolver([new StubProvider(EncryptionProviderNames.DotNet)]);

        Assert.Null(resolver.Resolve(null!));
        Assert.Null(resolver.Resolve(""));
        Assert.Null(resolver.Resolve("   "));
    }

    private sealed class StubProvider(string name) : IEncryptionProvider
    {
        public string Name { get; } = name;

        public Task<EncryptionResult> EncryptAsync(
            string filePath,
            EncryptionPolicy policy,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(EncryptionResult.Success(0));
        }
    }
}
