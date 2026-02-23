using System.Text.Json;
using EasySave.Exceptions;
using EasySave.LogServer.Tests.TestHelpers;

namespace EasySave.LogServer.Tests;

public class EnrichedJsonLogFormatterTests
{
    private readonly EnrichedJsonLogFormatter _formatter = new();

    [Fact]
    public void Format_ThrowsOnNull()
    {
        Assert.Throws<InvalidArgumentException>(() => _formatter.Format(null!));
    }

    [Fact]
    public void Format_ReturnsValidJson()
    {
        var entry = EnrichedLogEntryFactory.Create();

        var json = _formatter.Format(entry);

        var doc = JsonDocument.Parse(json);
        Assert.NotNull(doc);
    }

    [Fact]
    public void Format_UsesCamelCasePropertyNames()
    {
        var entry = EnrichedLogEntryFactory.Create();

        var json = _formatter.Format(entry);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("backupName", out _));
        Assert.True(root.TryGetProperty("macAddress", out _));
        Assert.True(root.TryGetProperty("clientName", out _));
        Assert.True(root.TryGetProperty("fileSizeBytes", out _));
        Assert.True(root.TryGetProperty("transferTimeMs", out _));
    }

    [Fact]
    public void Format_ContainsAllFields()
    {
        var entry = EnrichedLogEntryFactory.Create();

        var json = _formatter.Format(entry);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(1, root.GetProperty("backupId").GetInt32());
        Assert.Equal("Backup-1", root.GetProperty("backupName").GetString());
        Assert.Equal("FileTransferred", root.GetProperty("eventType").GetString());
        Assert.Equal(1024, root.GetProperty("fileSizeBytes").GetInt64());
        Assert.Equal(50, root.GetProperty("transferTimeMs").GetInt64());
        Assert.Equal(10, root.GetProperty("encryptionTimeMs").GetInt64());
        Assert.Equal("AA:BB:CC:DD:EE:FF", root.GetProperty("macAddress").GetString());
        Assert.Equal("Poste-1", root.GetProperty("clientName").GetString());
    }

    [Fact]
    public void Format_NullClientName_SerializesAsNull()
    {
        var entry = EnrichedLogEntryFactory.Create(clientName: null);

        var json = _formatter.Format(entry);
        var doc = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("clientName").ValueKind);
    }

    [Fact]
    public void GetFileHeader_ReturnsBracket()
    {
        Assert.Equal("[", _formatter.GetFileHeader());
    }

    [Fact]
    public void GetFileFooter_ReturnsBracket()
    {
        Assert.Equal("]", _formatter.GetFileFooter());
    }

    [Fact]
    public void GetEntrySeparator_ReturnsComma()
    {
        Assert.Equal(",", _formatter.GetEntrySeparator());
    }

    [Fact]
    public void GetIndentSpaces_ReturnsTwo()
    {
        Assert.Equal(2, _formatter.GetIndentSpaces());
    }
}
