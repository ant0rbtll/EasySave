using System.Xml.Linq;
using LogEntry = EasySave.Log.LogEntry;
using LogEventType = EasySave.Log.LogEventType;

namespace EasyLog.Tests;

public class XmlLogFormatterTests
{
    [Fact]
    public void Format_ContainsAllFieldsWithCorrectStructure()
    {
        var entry = new LogEntry(
            new DateTime(2026, 1, 3, 8, 30, 0, DateTimeKind.Utc),
            "JobFmt",
            LogEventType.TransferFile,
            "src",
            "dst",
            10,
            5);

        var formatter = new XmlLogFormatter();
        var xml = formatter.Format(entry);

        var doc = XDocument.Parse(xml);
        var root = doc.Root;

        Assert.NotNull(root);
        Assert.Equal("LogEntry", root.Name.LocalName);
        
        Assert.NotNull(root.Element("Timestamp"));
        Assert.NotNull(root.Element("BackupName"));
        Assert.NotNull(root.Element("EventType"));
        Assert.NotNull(root.Element("SourcePathUNC"));
        Assert.NotNull(root.Element("DestinationPathUNC"));
        Assert.NotNull(root.Element("FileSizeBytes"));
        Assert.NotNull(root.Element("TransferTimeMs"));
    }

    [Fact]
    public void Format_TimestampUsesISO8601Format()
    {
        var timestamp = new DateTime(2026, 1, 3, 8, 30, 45, DateTimeKind.Utc);
        var entry = new LogEntry(
            timestamp,
            "TestJob",
            LogEventType.TransferFile,
            "src",
            "dst",
            0,
            0);

        var formatter = new XmlLogFormatter();
        var xml = formatter.Format(entry);

        var doc = XDocument.Parse(xml);
        var timestampElement = doc.Root?.Element("Timestamp");

        Assert.NotNull(timestampElement);
        Assert.Equal("2026-01-03T08:30:45.0000000Z", timestampElement.Value);
    }

    [Fact]
    public void Format_EventTypeIsFormattedAsString()
    {
        var entry = new LogEntry(
            DateTime.UtcNow,
            "JobFmt",
            LogEventType.StartBackup,
            "src",
            "dst",
            0,
            0);

        var formatter = new XmlLogFormatter();
        var xml = formatter.Format(entry);

        var doc = XDocument.Parse(xml);
        var eventTypeElement = doc.Root?.Element("EventType");

        Assert.NotNull(eventTypeElement);
        Assert.Equal("StartBackup", eventTypeElement.Value);
    }

    [Fact]
    public void Format_NumericFieldsAreFormattedCorrectly()
    {
        var entry = new LogEntry(
            DateTime.UtcNow,
            "JobFmt",
            LogEventType.TransferFile,
            "src",
            "dst",
            1024,
            250);

        var formatter = new XmlLogFormatter();
        var xml = formatter.Format(entry);

        var doc = XDocument.Parse(xml);
        var fileSizeElement = doc.Root?.Element("FileSizeBytes");
        var transferTimeElement = doc.Root?.Element("TransferTimeMs");

        Assert.NotNull(fileSizeElement);
        Assert.NotNull(transferTimeElement);
        Assert.Equal("1024", fileSizeElement.Value);
        Assert.Equal("250", transferTimeElement.Value);
    }

    [Fact]
    public void Format_HandlesEmptyStrings()
    {
        var entry = new LogEntry(
            DateTime.UtcNow,
            "",
            LogEventType.TransferFile,
            "",
            "",
            0,
            0);

        var formatter = new XmlLogFormatter();
        var xml = formatter.Format(entry);

        var doc = XDocument.Parse(xml);
        
        Assert.NotNull(doc.Root);
        Assert.Empty(doc.Root.Element("BackupName")?.Value ?? "");
        Assert.Empty(doc.Root.Element("SourcePathUNC")?.Value ?? "");
        Assert.Empty(doc.Root.Element("DestinationPathUNC")?.Value ?? "");
    }

    [Fact]
    public void GetFileHeader_ReturnsValidXmlDeclarationWithLogsRoot()
    {
        var formatter = new XmlLogFormatter();
        var header = formatter.GetFileHeader();

        Assert.Contains("<?xml version=\"1.0\" encoding=\"utf-8\"?>", header);
        Assert.Contains("<Logs>", header);
    }

    [Fact]
    public void GetFileFooter_ReturnsClosingLogsTag()
    {
        var formatter = new XmlLogFormatter();
        var footer = formatter.GetFileFooter();

        Assert.Equal("</Logs>", footer);
    }

    [Fact]
    public void GetEntrySeparator_ReturnsEmpty()
    {
        var formatter = new XmlLogFormatter();
        var separator = formatter.GetEntrySeparator();

        Assert.Equal(string.Empty, separator);
    }

    [Fact]
    public void GetIndentSpaces_ReturnsTwo()
    {
        var formatter = new XmlLogFormatter();
        var indent = formatter.GetIndentSpaces();

        Assert.Equal(2, indent);
    }

    [Fact]
    public void Format_ThrowsArgumentNullException_WhenEntryIsNull()
    {
        var formatter = new XmlLogFormatter();

        Assert.Throws<ArgumentNullException>(() => formatter.Format(null!));
    }

    [Fact]
    public void Format_ProducesValidXml()
    {
        var entry = new LogEntry(
            DateTime.UtcNow,
            "TestJob",
            LogEventType.TransferFile,
            "C:\\source\\file.txt",
            "D:\\backup\\file.txt",
            2048,
            150);

        var formatter = new XmlLogFormatter();
        var xml = formatter.Format(entry);

        // Should not throw
        var doc = XDocument.Parse(xml);
        Assert.NotNull(doc.Root);
    }

    [Fact]
    public void Format_HandlesSpecialXmlCharacters()
    {
        var entry = new LogEntry(
            DateTime.UtcNow,
            "Job<Test>&\"Name\"",
            LogEventType.TransferFile,
            "C:\\path\\with\\<special>&chars.txt",
            "D:\\backup\\file.txt",
            0,
            0);

        var formatter = new XmlLogFormatter();
        var xml = formatter.Format(entry);

        var doc = XDocument.Parse(xml);
        var backupName = doc.Root?.Element("BackupName")?.Value;

        Assert.NotNull(backupName);
        Assert.Equal("Job<Test>&\"Name\"", backupName);
    }
}
