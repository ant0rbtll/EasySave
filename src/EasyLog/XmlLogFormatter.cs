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
    /// <summary>
    /// Formats a single log entry as an XML element.
    /// </summary>
    /// <param name="entry">Entry to format.</param>
    /// <returns>Formatted XML fragment string.</returns>
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

    /// <summary>
    /// Gets the XML declaration and root opening tag for log files.
    /// </summary>
    /// <returns>XML file header.</returns>
    public string GetFileHeader() => "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Logs>";

    /// <summary>
    /// Gets the XML root closing tag for log files.
    /// </summary>
    /// <returns>XML file footer.</returns>
    public string GetFileFooter() => "</Logs>";

    /// <summary>
    /// Gets the separator token between entries.
    /// </summary>
    /// <returns>An empty string for XML layout.</returns>
    public string GetEntrySeparator() => string.Empty;

    /// <summary>
    /// Gets indentation width used by the logger file writer.
    /// </summary>
    /// <returns>Indentation width in spaces.</returns>
    public int GetIndentSpaces() => 2;
}
