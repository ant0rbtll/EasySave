using System.Text;
using System.Xml;
using System.Xml.Linq;
using EasySave.LogServer.Models;
using EasyLog;
using EasySave.Core.Exceptions;

namespace EasySave.LogServer.Services;

/// <summary>
/// Formats enriched log entries as XML strings.
/// Mirrors the style of <see cref="XmlLogFormatter"/> with additional MAC/client fields.
/// </summary>
public sealed class EnrichedXmlLogFormatter : ILogFileLayout
{
    public string Format(EnrichedLogEntry entry)
    {
        EasysaveDefaultException.ThrowIfNull(entry);

        var logEntry = new XElement("LogEntry",
            new XElement("Timestamp", entry.Timestamp.ToString("o")),
            new XElement("BackupName", entry.BackupName),
            new XElement("BackupId", entry.BackupId),
            new XElement("EventType", entry.EventType),
            new XElement("SourcePathUNC", entry.SourcePathUNC),
            new XElement("DestinationPathUNC", entry.DestinationPathUNC),
            new XElement("FileSizeBytes", entry.FileSizeBytes),
            new XElement("TransferTimeMs", entry.TransferTimeMs),
            new XElement("EncryptionTimeMs", entry.EncryptionTimeMs),
            new XElement("MacAddress", entry.MacAddress),
            new XElement("ClientName", entry.ClientName ?? string.Empty)
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
