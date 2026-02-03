using System.Text.Json;

namespace EasyLog.Tests;

public class JsonLogFormatterTests
{
    [Fact]
    public void Format_UsesCamelCaseAndContainsAllFields()
    {
        var entry = new LogEntry(
            new DateTime(2026, 1, 3, 8, 30, 0, DateTimeKind.Utc),
            "JobFmt",
            LogEventType.TransferFile,
            "src",
            "dst",
            10,
            5);

        var formatter = new JsonLogFormatter();
        var json = formatter.Format(entry);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("timestamp", out _));
        Assert.True(root.TryGetProperty("backupName", out _));
        Assert.True(root.TryGetProperty("eventType", out _));
        Assert.True(root.TryGetProperty("sourcePathUNC", out _));
        Assert.True(root.TryGetProperty("destinationPathUNC", out _));
        Assert.True(root.TryGetProperty("fileSizeBytes", out _));
        Assert.True(root.TryGetProperty("transferTimeMs", out _));
    }
}
