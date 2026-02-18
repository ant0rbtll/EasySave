using System.Text.Json;
using EasySave.LogServer.Tests.TestHelpers;

namespace EasySave.LogServer.Tests;

public class ServerLogWriterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ServerPathProvider _pathProvider;

    public ServerLogWriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"logserver_tests_{Guid.NewGuid():N}");
        _pathProvider = new ServerPathProvider(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Constructor_ThrowsOnNullPathProvider()
    {
        Assert.Throws<ArgumentNullException>(() => new ServerLogWriter(null!, LogFormat.Json));
    }

    [Fact]
    public void Write_ThrowsOnNullEntry()
    {
        using var writer = new ServerLogWriter(_pathProvider, LogFormat.Json);

        Assert.Throws<ArgumentNullException>(() => writer.Write(null!));
    }

    [Fact]
    public void Write_Json_CreatesFileWithValidJsonArray()
    {
        using var writer = new ServerLogWriter(_pathProvider, LogFormat.Json);
        var entry = EnrichedLogEntryFactory.Create();

        writer.Write(entry);

        var path = _pathProvider.GetDailyLogPath(entry.Timestamp.Date, LogFormat.Json);
        var content = File.ReadAllText(path);
        var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public void Write_Json_MultipleEntries_ProducesValidArray()
    {
        using var writer = new ServerLogWriter(_pathProvider, LogFormat.Json);
        var date = new DateTime(2025, 3, 15, 10, 0, 0, DateTimeKind.Utc);

        writer.Write(EnrichedLogEntryFactory.Create(timestamp: date, backupName: "Backup-1"));
        writer.Write(EnrichedLogEntryFactory.Create(timestamp: date.AddMinutes(5), backupName: "Backup-2"));
        writer.Write(EnrichedLogEntryFactory.Create(timestamp: date.AddMinutes(10), backupName: "Backup-3"));

        var path = _pathProvider.GetDailyLogPath(date.Date, LogFormat.Json);
        var content = File.ReadAllText(path);
        var doc = JsonDocument.Parse(content);
        Assert.Equal(3, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public void Write_Json_AppendsToExistingFile()
    {
        using var writer = new ServerLogWriter(_pathProvider, LogFormat.Json);
        var date = new DateTime(2025, 3, 15, 10, 0, 0, DateTimeKind.Utc);

        writer.Write(EnrichedLogEntryFactory.Create(timestamp: date, backupName: "First"));

        var path = _pathProvider.GetDailyLogPath(date.Date, LogFormat.Json);
        var contentAfterFirst = File.ReadAllText(path);
        var docFirst = JsonDocument.Parse(contentAfterFirst);
        Assert.Equal(1, docFirst.RootElement.GetArrayLength());

        writer.Write(EnrichedLogEntryFactory.Create(timestamp: date.AddMinutes(5), backupName: "Second"));

        var contentAfterSecond = File.ReadAllText(path);
        var docSecond = JsonDocument.Parse(contentAfterSecond);
        Assert.Equal(2, docSecond.RootElement.GetArrayLength());
        Assert.Equal("Second", docSecond.RootElement[1].GetProperty("backupName").GetString());
    }

    [Fact]
    public void Write_Json_WritesToExistingEmptyFile()
    {
        var date = new DateTime(2025, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        var path = _pathProvider.GetDailyLogPath(date.Date, LogFormat.Json);
        File.WriteAllText(path, string.Empty);

        using var writer = new ServerLogWriter(_pathProvider, LogFormat.Json);
        writer.Write(EnrichedLogEntryFactory.Create(timestamp: date));

        var content = File.ReadAllText(path);
        var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public void Write_Xml_CreatesFileWithValidXml()
    {
        using var writer = new ServerLogWriter(_pathProvider, LogFormat.Xml);
        var entry = EnrichedLogEntryFactory.Create();

        writer.Write(entry);

        var path = _pathProvider.GetDailyLogPath(entry.Timestamp.Date, LogFormat.Xml);
        var content = File.ReadAllText(path);
        Assert.Contains("<?xml", content);
        Assert.Contains("<Logs>", content);
        Assert.Contains("</Logs>", content);
        Assert.Contains("<LogEntry>", content);
    }

    [Fact]
    public void Write_Xml_MultipleEntries_ProducesValidXml()
    {
        using var writer = new ServerLogWriter(_pathProvider, LogFormat.Xml);
        var date = new DateTime(2025, 3, 15, 10, 0, 0, DateTimeKind.Utc);

        writer.Write(EnrichedLogEntryFactory.Create(timestamp: date, backupName: "Backup-1"));
        writer.Write(EnrichedLogEntryFactory.Create(timestamp: date.AddMinutes(5), backupName: "Backup-2"));

        var path = _pathProvider.GetDailyLogPath(date.Date, LogFormat.Xml);
        var content = File.ReadAllText(path);
        Assert.Contains("<Logs>", content);
        Assert.Contains("</Logs>", content);

        var count = content.Split("<LogEntry>").Length - 1;
        Assert.Equal(2, count);
    }

    [Fact]
    public void Write_Xml_AppendsToExistingFile()
    {
        using var writer = new ServerLogWriter(_pathProvider, LogFormat.Xml);
        var date = new DateTime(2025, 3, 15, 10, 0, 0, DateTimeKind.Utc);

        writer.Write(EnrichedLogEntryFactory.Create(timestamp: date, backupName: "First"));
        writer.Write(EnrichedLogEntryFactory.Create(timestamp: date.AddMinutes(5), backupName: "Second"));

        var path = _pathProvider.GetDailyLogPath(date.Date, LogFormat.Xml);
        var content = File.ReadAllText(path);

        Assert.Contains("<Logs>", content);
        Assert.Contains("</Logs>", content);
        Assert.Contains("First", content);
        Assert.Contains("Second", content);

        var singleLogsOpen = content.Split("<Logs>").Length - 1;
        var singleLogsClose = content.Split("</Logs>").Length - 1;
        Assert.Equal(1, singleLogsOpen);
        Assert.Equal(1, singleLogsClose);
    }

    [Fact]
    public void Write_RequestedFormat_OverridesDefault()
    {
        using var writer = new ServerLogWriter(_pathProvider, LogFormat.Json);
        var entry = EnrichedLogEntryFactory.Create();

        writer.Write(entry, LogFormat.Xml);

        var xmlPath = _pathProvider.GetDailyLogPath(entry.Timestamp.Date, LogFormat.Xml);
        var jsonPath = _pathProvider.GetDailyLogPath(entry.Timestamp.Date, LogFormat.Json);
        Assert.True(File.Exists(xmlPath));
        Assert.False(File.Exists(jsonPath));
    }

    [Fact]
    public void Write_NormalizesPathsToBackslash()
    {
        using var writer = new ServerLogWriter(_pathProvider, LogFormat.Json);
        var entry = EnrichedLogEntryFactory.Create(sourcePath: "C:/Source/file.txt", destPath: "D:/Dest/file.txt");

        writer.Write(entry);

        var path = _pathProvider.GetDailyLogPath(entry.Timestamp.Date, LogFormat.Json);
        var content = File.ReadAllText(path);
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement[0];
        Assert.Equal(@"C:\Source\file.txt", root.GetProperty("sourcePathUNC").GetString());
        Assert.Equal(@"D:\Dest\file.txt", root.GetProperty("destinationPathUNC").GetString());
    }

    [Fact]
    public void Write_NormalizesTimestampToUtc()
    {
        using var writer = new ServerLogWriter(_pathProvider, LogFormat.Json);
        var localTime = new DateTime(2025, 3, 15, 10, 30, 0, DateTimeKind.Unspecified);
        var entry = EnrichedLogEntryFactory.Create(timestamp: localTime);

        writer.Write(entry);

        var path = _pathProvider.GetDailyLogPath(localTime.Date, LogFormat.Json);
        var content = File.ReadAllText(path);
        Assert.Contains("2025-03-15T10:30:00Z", content);
    }

    [Fact]
    public void Write_DifferentDates_CreatesSeparateFiles()
    {
        using var writer = new ServerLogWriter(_pathProvider, LogFormat.Json);
        var date1 = new DateTime(2025, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        var date2 = new DateTime(2025, 3, 16, 10, 0, 0, DateTimeKind.Utc);

        writer.Write(EnrichedLogEntryFactory.Create(timestamp: date1));
        writer.Write(EnrichedLogEntryFactory.Create(timestamp: date2));

        var path1 = _pathProvider.GetDailyLogPath(date1.Date, LogFormat.Json);
        var path2 = _pathProvider.GetDailyLogPath(date2.Date, LogFormat.Json);
        Assert.True(File.Exists(path1));
        Assert.True(File.Exists(path2));
    }

    [Fact]
    public void Write_EmptyPaths_HandledGracefully()
    {
        using var writer = new ServerLogWriter(_pathProvider, LogFormat.Json);
        var entry = EnrichedLogEntryFactory.Create(sourcePath: "", destPath: "   ");

        var ex = Record.Exception(() => writer.Write(entry));

        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var writer = new ServerLogWriter(_pathProvider, LogFormat.Json);

        writer.Dispose();
        var ex = Record.Exception(() => writer.Dispose());

        Assert.Null(ex);
    }
}
