using System.Text;
using System.Xml;
using System.Xml.Linq;
using EasySave.Log;

namespace EasyLog;

/// <summary>
/// Formats log entries as XML strings.
/// </summary>
public sealed class XmlLogFormatter : ILogFormatter, ILogFileLayout
{
    public string Format(EasySave.Log.LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var logEntry = new XElement("LogEntry",
            new XElement("Timestamp", entry.Timestamp.ToString("o")),
            new XElement("BackupName", entry.BackupName),
            new XElement("EventType", entry.EventType.ToString()),
            new XElement("SourcePathUNC", entry.SourcePathUNC),
            new XElement("DestinationPathUNC", entry.DestinationPathUNC),
            new XElement("FileSizeBytes", entry.FileSizeBytes),
            new XElement("TransferTimeMs", entry.TransferTimeMs)
        );

        var settings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = true,
            Encoding = Encoding.UTF8
        };

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        logEntry.WriteTo(xmlWriter);
        xmlWriter.Flush();

        return stringWriter.ToString();
    }

    public string GetFileHeader() => "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Logs>";

    public string GetFileFooter() => "</Logs>";

    public string GetEntrySeparator() => string.Empty;

    public int GetIndentSpaces() => 2;
}
