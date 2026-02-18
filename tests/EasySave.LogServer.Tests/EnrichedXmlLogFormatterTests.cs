using System.Xml.Linq;
using EasySave.LogServer.Tests.TestHelpers;

namespace EasySave.LogServer.Tests;

public class EnrichedXmlLogFormatterTests
{
    private readonly EnrichedXmlLogFormatter _formatter = new();

    [Fact]
    public void Format_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => _formatter.Format(null!));
    }

    [Fact]
    public void Format_ReturnsValidXml()
    {
        var entry = EnrichedLogEntryFactory.Create();

        var xml = _formatter.Format(entry);

        var element = XElement.Parse(xml);
        Assert.Equal("LogEntry", element.Name.LocalName);
    }

    [Fact]
    public void Format_ContainsAllElements()
    {
        var entry = EnrichedLogEntryFactory.Create();

        var xml = _formatter.Format(entry);
        var element = XElement.Parse(xml);

        Assert.NotNull(element.Element("Timestamp"));
        Assert.NotNull(element.Element("BackupName"));
        Assert.NotNull(element.Element("BackupId"));
        Assert.NotNull(element.Element("EventType"));
        Assert.NotNull(element.Element("SourcePathUNC"));
        Assert.NotNull(element.Element("DestinationPathUNC"));
        Assert.NotNull(element.Element("FileSizeBytes"));
        Assert.NotNull(element.Element("TransferTimeMs"));
        Assert.NotNull(element.Element("EncryptionTimeMs"));
        Assert.NotNull(element.Element("MacAddress"));
        Assert.NotNull(element.Element("ClientName"));
    }

    [Fact]
    public void Format_ContainsCorrectValues()
    {
        var entry = EnrichedLogEntryFactory.Create();

        var xml = _formatter.Format(entry);
        var element = XElement.Parse(xml);

        Assert.Equal("Backup-1", element.Element("BackupName")!.Value);
        Assert.Equal("1", element.Element("BackupId")!.Value);
        Assert.Equal("FileTransferred", element.Element("EventType")!.Value);
        Assert.Equal("1024", element.Element("FileSizeBytes")!.Value);
        Assert.Equal("50", element.Element("TransferTimeMs")!.Value);
        Assert.Equal("10", element.Element("EncryptionTimeMs")!.Value);
        Assert.Equal("AA:BB:CC:DD:EE:FF", element.Element("MacAddress")!.Value);
        Assert.Equal("Poste-1", element.Element("ClientName")!.Value);
    }

    [Fact]
    public void Format_NullClientName_SerializesAsEmpty()
    {
        var entry = EnrichedLogEntryFactory.Create(clientName: null);

        var xml = _formatter.Format(entry);
        var element = XElement.Parse(xml);

        Assert.Equal(string.Empty, element.Element("ClientName")!.Value);
    }

    [Fact]
    public void GetFileHeader_ContainsXmlDeclarationAndLogsTag()
    {
        var header = _formatter.GetFileHeader();

        Assert.Contains("<?xml", header);
        Assert.Contains("<Logs>", header);
    }

    [Fact]
    public void GetFileFooter_ReturnsClosingLogsTag()
    {
        Assert.Equal("</Logs>", _formatter.GetFileFooter());
    }

    [Fact]
    public void GetEntrySeparator_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _formatter.GetEntrySeparator());
    }

    [Fact]
    public void GetIndentSpaces_ReturnsTwo()
    {
        Assert.Equal(2, _formatter.GetIndentSpaces());
    }
}
