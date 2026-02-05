using System.Text.Json;
using EasyLog;
using EasySave.Log;

namespace EasySave.Log.Tests;

public class JsonLogFormatterTests
{
    [Fact]
    public void Format_UsesCamelCaseAndContainsAllFields()
    {
        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 8, 30, 0, DateTimeKind.Utc),
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

    [Fact]
    public void Format_ThrowsOnNullEntry()
    {
        var formatter = new JsonLogFormatter();

        Assert.Throws<ArgumentNullException>(() => formatter.Format(null!));
    }

    [Fact]
    public void Format_SerializesEnumAsNumber()
    {
        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 8, 30, 0, DateTimeKind.Utc),
            "JobEnum",
            LogEventType.Error,
            "src",
            "dst",
            10,
            5);

        var formatter = new JsonLogFormatter();
        var json = formatter.Format(entry);

        using var doc = JsonDocument.Parse(json);
        var eventType = doc.RootElement.GetProperty("eventType");

        Assert.Equal(JsonValueKind.Number, eventType.ValueKind);
        Assert.Equal((int)LogEventType.Error, eventType.GetInt32());
    }

    [Fact]
    public void Format_RoundTripsToLogEntry()
    {
        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 8, 30, 0, DateTimeKind.Utc),
            "JobRoundTrip",
            LogEventType.TransferFile,
            "\\\\server\\src",
            "\\\\server\\dst",
            123,
            456);

        var formatter = new JsonLogFormatter();
        var json = formatter.Format(entry);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var roundTripped = JsonSerializer.Deserialize<LogEntry>(json, options);

        Assert.NotNull(roundTripped);
        Assert.Equal(entry, roundTripped);
    }

    [Fact]
    public void Format_UsesUnsafeRelaxedJsonEscaping()
    {
        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 8, 30, 0, DateTimeKind.Utc),
            "<script>alert('x')</script>",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            1);

        var formatter = new JsonLogFormatter();
        var json = formatter.Format(entry);

        Assert.Contains("<script>", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u003c", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\u003e", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Format_UtcTimestampContainsZ()
    {
        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 8, 30, 0, DateTimeKind.Utc),
            "JobUtc",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            1);

        var formatter = new JsonLogFormatter();
        var json = formatter.Format(entry);

        using var doc = JsonDocument.Parse(json);
        var timestamp = doc.RootElement.GetProperty("timestamp").GetString();

        Assert.NotNull(timestamp);
        Assert.EndsWith("Z", timestamp, StringComparison.Ordinal);
    }
}
