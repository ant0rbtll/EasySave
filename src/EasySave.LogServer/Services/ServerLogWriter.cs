using System.Text;
using EasySave.Core;
using EasySave.LogServer.Models;
using EasyLog;
using EasySave.Exceptions;

namespace EasySave.LogServer.Services;

/// <summary>
/// Writes enriched log entries to daily files with cross-process synchronization.
/// Replicates the file-append logic from DailyFileLogger for EnrichedLogEntry.
/// </summary>
public sealed class ServerLogWriter(ServerPathProvider pathProvider, LogFormat defaultFormat) : IDisposable
{
    private readonly ServerPathProvider _pathProvider = pathProvider ?? throw new InvalidArgumentException(nameof(pathProvider));
    private readonly LogFormat _defaultFormat = defaultFormat;
    private readonly EnrichedJsonLogFormatter _jsonFormatter = new();
    private readonly EnrichedXmlLogFormatter _xmlFormatter = new();
    private readonly Mutex _mutex = new(false, "Global\\ProSoft_EasySave_LogServer_DailyFile");
    private readonly object _sync = new();

    public void Write(EnrichedLogEntry entry, LogFormat? requestedFormat = null)
    {
        EasysaveDefaultException.ThrowIfNull(entry);

        var format = requestedFormat ?? _defaultFormat;
        var layout = GetLayout(format);
        var normalized = NormalizeEntry(entry);
        var path = _pathProvider.GetDailyLogPath(normalized.Timestamp.Date, format);

        var formattedEntry = format == LogFormat.Xml
            ? _xmlFormatter.Format(normalized)
            : _jsonFormatter.Format(normalized);

        lock (_sync)
        {
            if (!_mutex.WaitOne(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Unable to acquire log file mutex within timeout.");

            try
            {
                AppendEntryToFile(path, formattedEntry, layout);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }
    }

    private ILogFileLayout GetLayout(LogFormat format)
        => format == LogFormat.Xml ? _xmlFormatter : _jsonFormatter;

    public void Dispose()
    {
        _mutex.Dispose();
    }

    private static EnrichedLogEntry NormalizeEntry(EnrichedLogEntry e)
    {
        var ts = e.Timestamp.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(e.Timestamp, DateTimeKind.Utc)
            : e.Timestamp.ToUniversalTime();

        return e with
        {
            Timestamp = ts,
            SourcePathUNC = NormalizePath(e.SourcePathUNC),
            DestinationPathUNC = NormalizePath(e.DestinationPathUNC)
        };
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return path.Trim().Replace('/', '\\');
    }

    private static void AppendEntryToFile(string path, string formattedEntry, ILogFileLayout layout)
    {
        var indentedEntry = IndentBlock(formattedEntry, layout.GetIndentSpaces());

        if (!File.Exists(path))
        {
            CreateNewLogFile(path, indentedEntry, layout);
            return;
        }

        bool isEmptyFile;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
        {
            isEmptyFile = stream.Length == 0;
            if (!isEmptyFile)
                AppendToExistingFile(stream, indentedEntry, layout);
        }

        if (isEmptyFile)
            CreateNewLogFile(path, indentedEntry, layout);
    }

    private static void CreateNewLogFile(string path, string indentedEntry, ILogFileLayout layout)
    {
        var header = layout.GetFileHeader();
        var footer = layout.GetFileFooter();

        var content = new StringBuilder();
        if (!string.IsNullOrEmpty(header))
            content.AppendLine(header);
        content.AppendLine(indentedEntry);
        if (!string.IsNullOrEmpty(footer))
            content.AppendLine(footer);

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, content.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void AppendToExistingFile(FileStream stream, string indentedEntry, ILogFileLayout layout)
    {
        var footer = layout.GetFileFooter();
        var separator = layout.GetEntrySeparator();

        if (string.IsNullOrEmpty(footer))
        {
            AppendWithoutFooter(stream, separator, indentedEntry);
            return;
        }

        AppendBeforeFooter(stream, footer, separator, indentedEntry, layout);
    }

    private static void AppendBeforeFooter(FileStream stream, string footer, string separator, string indentedEntry, ILogFileLayout layout)
    {
        var footerBytes = Encoding.UTF8.GetBytes(footer);
        var bytes = ReadAllBytes(stream);
        int footerIndex = FindFooterAtEnd(bytes, footerBytes);

        if (footerIndex < 0)
        {
            stream.Seek(0, SeekOrigin.End);
            WriteNewLineIfNeeded(stream);
            if (!string.IsNullOrEmpty(separator))
                WriteUtf8(stream, separator + Environment.NewLine);
            WriteUtf8(stream, indentedEntry + Environment.NewLine + footer + Environment.NewLine);
            return;
        }

        var beforeFooter = Encoding.UTF8.GetString(bytes, 0, footerIndex).TrimEnd();
        bool hasExistingEntries = CheckIfHasExistingEntries(beforeFooter, layout);

        int truncateLength = Encoding.UTF8.GetByteCount(beforeFooter);
        stream.SetLength(truncateLength);
        stream.Seek(0, SeekOrigin.End);

        WriteUtf8(stream, Environment.NewLine);
        if (hasExistingEntries && !string.IsNullOrEmpty(separator))
            WriteUtf8(stream, separator + Environment.NewLine);

        WriteUtf8(stream, indentedEntry + Environment.NewLine + footer + Environment.NewLine);
    }

    private static bool CheckIfHasExistingEntries(string beforeFooter, ILogFileLayout layout)
    {
        var header = layout.GetFileHeader();

        if (!string.IsNullOrEmpty(header))
        {
            var trimmedBeforeFooter = beforeFooter.TrimStart();
            var normalizedHeader = header.Trim();
            if (!trimmedBeforeFooter.StartsWith(normalizedHeader, StringComparison.Ordinal))
                return !string.IsNullOrWhiteSpace(trimmedBeforeFooter);

            var afterHeader = trimmedBeforeFooter.Substring(normalizedHeader.Length).Trim();
            return !string.IsNullOrEmpty(afterHeader);
        }

        return !string.IsNullOrEmpty(beforeFooter.Trim());
    }

    private static void AppendWithoutFooter(FileStream stream, string separator, string indentedEntry)
    {
        stream.Seek(0, SeekOrigin.End);
        WriteNewLineIfNeeded(stream);
        if (!string.IsNullOrEmpty(separator))
            WriteUtf8(stream, separator + Environment.NewLine);
        WriteUtf8(stream, indentedEntry + Environment.NewLine);
    }

    private static byte[] ReadAllBytes(FileStream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);

        if (stream.Length > int.MaxValue)
            throw new IOException("Log file is too large to read into memory.");

        int length = checked((int)stream.Length);
        var bytes = new byte[length];

        int offset = 0;
        while (offset < length)
        {
            int bytesRead = stream.Read(bytes, offset, length - offset);
            if (bytesRead == 0)
                throw new IOException("Unexpected end of stream while reading log file.");
            offset += bytesRead;
        }

        return bytes;
    }

    private static int FindFooterAtEnd(byte[] bytes, byte[] footer)
    {
        if (footer.Length == 0 || bytes.Length < footer.Length)
            return -1;

        int end = bytes.Length - 1;
        while (end >= 0 && IsAsciiWhitespace(bytes[end]))
            end--;

        if (end < 0 || end + 1 < footer.Length)
            return -1;

        int start = end - footer.Length + 1;
        for (int i = 0; i < footer.Length; i++)
        {
            if (bytes[start + i] != footer[i])
                return -1;
        }

        return start;
    }

    private static void WriteNewLineIfNeeded(FileStream stream)
    {
        if (stream.Length == 0)
            return;

        stream.Seek(-1, SeekOrigin.End);
        int last = stream.ReadByte();
        stream.Seek(0, SeekOrigin.End);

        if (last != '\n' && last != '\r')
            WriteUtf8(stream, Environment.NewLine);
    }

    private static void WriteUtf8(FileStream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static bool IsAsciiWhitespace(byte value)
        => value is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r';

    private static string IndentBlock(string text, int spaces)
    {
        var indent = new string(' ', spaces);
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        for (int i = 0; i < lines.Length; i++)
            lines[i] = indent + lines[i];

        return string.Join(Environment.NewLine, lines);
    }
}
