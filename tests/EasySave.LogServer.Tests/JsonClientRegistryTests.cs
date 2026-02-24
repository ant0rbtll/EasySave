using EasySave.Exceptions;

namespace EasySave.LogServer.Tests;

public class JsonClientRegistryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;

    public JsonClientRegistryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"logserver_tests_{Guid.NewGuid():N}");
        _filePath = Path.Combine(_tempDir, "clients.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsOnInvalidPath(string? path)
    {
        Assert.ThrowsAny<InvalidArgumentException>(() => new JsonClientRegistry(path!));
    }

    [Fact]
    public void Constructor_CreatesParentDirectory()
    {
        using var registry = new JsonClientRegistry(_filePath);

        Assert.True(Directory.Exists(_tempDir));
    }

    [Fact]
    public void Constructor_HandlesNonExistentFile()
    {
        using var registry = new JsonClientRegistry(_filePath);

        var all = registry.GetAll();
        Assert.Empty(all);
    }

    [Fact]
    public void EnsureRegistered_AddsNewMac()
    {
        using var registry = new JsonClientRegistry(_filePath);

        registry.EnsureRegistered("AA:BB:CC:DD:EE:FF");

        var all = registry.GetAll();
        Assert.Single(all);
        Assert.Equal("AA:BB:CC:DD:EE:FF", all[0].MacAddress);
        Assert.Null(all[0].FriendlyName);
    }

    [Fact]
    public void EnsureRegistered_DoesNotDuplicateExistingMac()
    {
        using var registry = new JsonClientRegistry(_filePath);

        registry.EnsureRegistered("AA:BB:CC:DD:EE:FF");
        registry.EnsureRegistered("AA:BB:CC:DD:EE:FF");

        Assert.Single(registry.GetAll());
    }

    [Fact]
    public void EnsureRegistered_NormalizesMacToUpperCase()
    {
        using var registry = new JsonClientRegistry(_filePath);

        registry.EnsureRegistered("aa:bb:cc:dd:ee:ff");

        var all = registry.GetAll();
        Assert.Single(all);
        Assert.Equal("AA:BB:CC:DD:EE:FF", all[0].MacAddress);
    }

    [Fact]
    public void EnsureRegistered_CaseInsensitiveDeduplication()
    {
        using var registry = new JsonClientRegistry(_filePath);

        registry.EnsureRegistered("AA:BB:CC:DD:EE:FF");
        registry.EnsureRegistered("aa:bb:cc:dd:ee:ff");

        Assert.Single(registry.GetAll());
    }

    [Fact]
    public void GetFriendlyName_ReturnsNullForUnknownMac()
    {
        using var registry = new JsonClientRegistry(_filePath);

        var name = registry.GetFriendlyName("00:11:22:33:44:55");

        Assert.Null(name);
    }

    [Fact]
    public void GetFriendlyName_ReturnsNullForRegisteredWithoutName()
    {
        using var registry = new JsonClientRegistry(_filePath);
        registry.EnsureRegistered("AA:BB:CC:DD:EE:FF");

        var name = registry.GetFriendlyName("AA:BB:CC:DD:EE:FF");

        Assert.Null(name);
    }

    [Fact]
    public void SetFriendlyName_SetsAndRetrieves()
    {
        using var registry = new JsonClientRegistry(_filePath);
        registry.EnsureRegistered("AA:BB:CC:DD:EE:FF");

        registry.SetFriendlyName("AA:BB:CC:DD:EE:FF", "Poste-1");

        Assert.Equal("Poste-1", registry.GetFriendlyName("AA:BB:CC:DD:EE:FF"));
    }

    [Fact]
    public void SetFriendlyName_CaseInsensitiveMacLookup()
    {
        using var registry = new JsonClientRegistry(_filePath);
        registry.EnsureRegistered("aa:bb:cc:dd:ee:ff");

        registry.SetFriendlyName("AA:BB:CC:DD:EE:FF", "Poste-1");

        Assert.Equal("Poste-1", registry.GetFriendlyName("aa:bb:cc:dd:ee:ff"));
    }

    [Fact]
    public void SetFriendlyName_UnregisteredMac_AutoRegisters()
    {
        using var registry = new JsonClientRegistry(_filePath);

        registry.SetFriendlyName("AA:BB:CC:DD:EE:FF", "Poste-1");

        var all = registry.GetAll();
        Assert.Single(all);
        Assert.Equal("AA:BB:CC:DD:EE:FF", all[0].MacAddress);
        Assert.Equal("Poste-1", all[0].FriendlyName);
    }

    [Fact]
    public void SetFriendlyName_OverwritesPreviousName()
    {
        using var registry = new JsonClientRegistry(_filePath);
        registry.EnsureRegistered("AA:BB:CC:DD:EE:FF");
        registry.SetFriendlyName("AA:BB:CC:DD:EE:FF", "Poste-1");

        registry.SetFriendlyName("AA:BB:CC:DD:EE:FF", "Poste-2");

        Assert.Equal("Poste-2", registry.GetFriendlyName("AA:BB:CC:DD:EE:FF"));
    }

    [Fact]
    public void GetAll_ReturnsOrderedByMacAddress()
    {
        using var registry = new JsonClientRegistry(_filePath);
        registry.EnsureRegistered("CC:CC:CC:CC:CC:CC");
        registry.EnsureRegistered("AA:AA:AA:AA:AA:AA");
        registry.EnsureRegistered("BB:BB:BB:BB:BB:BB");

        var all = registry.GetAll();

        Assert.Equal(3, all.Count);
        Assert.Equal("AA:AA:AA:AA:AA:AA", all[0].MacAddress);
        Assert.Equal("BB:BB:BB:BB:BB:BB", all[1].MacAddress);
        Assert.Equal("CC:CC:CC:CC:CC:CC", all[2].MacAddress);
    }

    [Fact]
    public void Persistence_DataSurvivedReload()
    {
        using (var registry = new JsonClientRegistry(_filePath))
        {
            registry.EnsureRegistered("AA:BB:CC:DD:EE:FF");
            registry.SetFriendlyName("AA:BB:CC:DD:EE:FF", "Poste-1");
        }

        using var reloaded = new JsonClientRegistry(_filePath);
        var all = reloaded.GetAll();
        Assert.Single(all);
        Assert.Equal("AA:BB:CC:DD:EE:FF", all[0].MacAddress);
        Assert.Equal("Poste-1", all[0].FriendlyName);
    }

    [Fact]
    public void Persistence_HandlesCorruptedFile()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_filePath, "not valid json {{{{");

        using var registry = new JsonClientRegistry(_filePath);

        Assert.Empty(registry.GetAll());
    }

    [Fact]
    public void Persistence_PersistsAcrossMultipleOperations()
    {
        using (var registry = new JsonClientRegistry(_filePath))
        {
            registry.EnsureRegistered("AA:AA:AA:AA:AA:AA");
            registry.EnsureRegistered("BB:BB:BB:BB:BB:BB");
            registry.SetFriendlyName("AA:AA:AA:AA:AA:AA", "Client-A");
        }

        using var reloaded = new JsonClientRegistry(_filePath);
        Assert.Equal(2, reloaded.GetAll().Count);
        Assert.Equal("Client-A", reloaded.GetFriendlyName("AA:AA:AA:AA:AA:AA"));
        Assert.Null(reloaded.GetFriendlyName("BB:BB:BB:BB:BB:BB"));
    }
}
