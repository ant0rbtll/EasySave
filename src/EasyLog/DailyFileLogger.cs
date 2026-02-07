using System.Text;
using EasySave.Configuration;
using EasySave.Core;
using EasySave.Log;

namespace EasyLog;

/// <summary>
/// Logger that writes entries to a daily log file with cross-process synchronization.
/// </summary>
public sealed class DailyFileLogger : ILogger, IDisposable
{
    private readonly ILogFormatter _formatter;
    private readonly ILogFileLayout _layout;
    private readonly IPathProvider _pathProvider;
    private readonly LogFormat _format;

    private readonly object _sync = new();

    // Cross-process lock (useful if multiple EasySave processes run).
    private readonly Mutex _mutex;

    /// <summary>
    /// Initializes a new instance of the <see cref="DailyFileLogger"/> class.
    /// </summary>
    /// <param name="formatter">Entry formatter used for each log entry payload.</param>
    /// <param name="pathProvider">Path provider used to resolve the daily log file.</param>
    /// <param name="mutexName">Global mutex name used for cross-process synchronization.</param>
    /// <param name="format">Log format used to determine file extension.</param>
    /// <param name="layout">Optional file layout strategy. Defaults to formatter when it implements <see cref="ILogFileLayout"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> or <paramref name="pathProvider"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when no file layout strategy can be resolved.</exception>
    public DailyFileLogger(
        ILogFormatter formatter,
        IPathProvider pathProvider,
        string mutexName = "Global\\ProSoft_EasySave_EasyLog_DailyFile",
        LogFormat format = LogFormat.Json,
        ILogFileLayout? layout = null)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _layout = layout ?? formatter as ILogFileLayout
            ?? throw new ArgumentException("A log file layout must be provided.", nameof(layout));
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _format = format;
        _mutex = new Mutex(false, mutexName);
    }

    /// <summary>
    /// Writes one log entry to the daily file after normalization and synchronization.
    /// </summary>
    /// <param name="entry">Entry to write.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null.</exception>
    /// <exception cref="TimeoutException">Thrown when the file mutex cannot be acquired in time.</exception>
    public void Write(EasySave.Log.LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var normalized = NormalizeEntry(entry);
        var path = _pathProvider.GetDailyLogPath(normalized.Timestamp.Date, _format);

        EnsureDirectoryExists(path);

        var formattedEntry = _formatter.Format(normalized);

        lock (_sync)
        {
            if (!_mutex.WaitOne(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Unable to acquire log file mutex within timeout.");

            try
            {
                AppendEntryToFile(path, formattedEntry);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }
    }

    /// <summary>
    /// Releases the underlying cross-process mutex.
    /// </summary>
    public void Dispose()
    {
        _mutex.Dispose();
    }

    /// <summary>
    /// Normalizes timestamp and paths before persistence.
    /// </summary>
    /// <param name="e">Entry to normalize.</param>
    /// <returns>A normalized copy of the entry.</returns>
    private static EasySave.Log.LogEntry NormalizeEntry(EasySave.Log.LogEntry e)
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

    /// <summary>
    /// Trims and normalizes path separators to backslashes.
    /// </summary>
    /// <param name="path">Raw path value.</param>
    /// <returns>Normalized path or an empty string.</returns>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var p = path.Trim();
        p = p.Replace('/', '\\');
        return p;
    }

    /// <summary>
    /// Ensures the directory containing the target file exists.
    /// </summary>
    /// <param name="filePath">Target file path.</param>
    private static void EnsureDirectoryExists(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(dir))
            return;

        Directory.CreateDirectory(dir);
    }

    /// <summary>
    /// Appends one formatted entry to an existing file or creates the file when missing.
    /// </summary>
    /// <param name="path">Log file path.</param>
    /// <param name="formattedEntry">Formatted entry body.</param>
    private void AppendEntryToFile(string path, string formattedEntry)
    {
        var indentSpaces = _layout.GetIndentSpaces();
        var indentedEntry = IndentBlock(formattedEntry, indentSpaces);

        if (!File.Exists(path))
        {
            CreateNewLogFile(path, indentedEntry);
            return;
        }

        bool isEmptyFile;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
        {
            isEmptyFile = stream.Length == 0;
            if (!isEmptyFile)
                AppendToExistingFile(stream, indentedEntry);
        }

        if (isEmptyFile)
            CreateNewLogFile(path, indentedEntry);
    }

    /// <summary>
    /// Creates a new log file with optional header and footer.
    /// </summary>
    /// <param name="path">Log file path.</param>
    /// <param name="indentedEntry">Entry text already indented for file layout.</param>
    private void CreateNewLogFile(string path, string indentedEntry)
    {
        var header = _layout.GetFileHeader();
        var footer = _layout.GetFileFooter();

        var content = new StringBuilder();
        if (!string.IsNullOrEmpty(header))
            content.AppendLine(header);
        content.AppendLine(indentedEntry);
        if (!string.IsNullOrEmpty(footer))
            content.AppendLine(footer);

        File.WriteAllText(path, content.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>
    /// Appends an entry to an already existing file stream.
    /// </summary>
    /// <param name="stream">Opened file stream.</param>
    /// <param name="indentedEntry">Entry text already indented for file layout.</param>
    private void AppendToExistingFile(FileStream stream, string indentedEntry)
    {
        var footer = _layout.GetFileFooter();
        var separator = _layout.GetEntrySeparator();

        if (string.IsNullOrEmpty(footer))
        {
            AppendWithoutFooter(stream, separator, indentedEntry);
            return;
        }

        AppendBeforeFooter(stream, footer, separator, indentedEntry);
    }

    /// <summary>
    /// Inserts an entry right before the closing footer token.
    /// </summary>
    /// <param name="stream">Opened file stream.</param>
    /// <param name="footer">Footer token expected at the end of the file.</param>
    /// <param name="separator">Entry separator token.</param>
    /// <param name="indentedEntry">Entry text already indented for file layout.</param>
    private void AppendBeforeFooter(FileStream stream, string footer, string separator, string indentedEntry)
    {
        var footerBytes = Encoding.UTF8.GetBytes(footer);
        var bytes = ReadAllBytes(stream);
        int footerIndex = FindFooterAtEnd(bytes, footerBytes);

        if (footerIndex < 0)
        {
            // Keep backward-compatible behavior for malformed files: append and close with footer.
            stream.Seek(0, SeekOrigin.End);
            WriteNewLineIfNeeded(stream, bytes);
            if (!string.IsNullOrEmpty(separator))
                WriteUtf8(stream, separator + Environment.NewLine);
            WriteUtf8(stream, indentedEntry + Environment.NewLine + footer + Environment.NewLine);
            return;
        }

        var beforeFooter = Encoding.UTF8.GetString(bytes, 0, footerIndex).TrimEnd();
        bool hasExistingEntries = CheckIfHasExistingEntries(beforeFooter);

        int truncateLength = Encoding.UTF8.GetByteCount(beforeFooter);
        stream.SetLength(truncateLength);
        stream.Seek(0, SeekOrigin.End);

        WriteUtf8(stream, Environment.NewLine);
        if (hasExistingEntries && !string.IsNullOrEmpty(separator))
            WriteUtf8(stream, separator + Environment.NewLine);

        WriteUtf8(stream, indentedEntry + Environment.NewLine + footer + Environment.NewLine);
    }

    /// <summary>
    /// Determines whether the file already contains at least one persisted entry.
    /// </summary>
    /// <param name="beforeFooter">File content located before the footer.</param>
    /// <returns><c>true</c> when at least one entry exists; otherwise <c>false</c>.</returns>
    private bool CheckIfHasExistingEntries(string beforeFooter)
    {
        var header = _layout.GetFileHeader();

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

    /// <summary>
    /// Appends an entry when no footer is defined by the layout.
    /// </summary>
    /// <param name="stream">Opened file stream.</param>
    /// <param name="separator">Entry separator token.</param>
    /// <param name="indentedEntry">Entry text already indented for file layout.</param>
    private static void AppendWithoutFooter(FileStream stream, string separator, string indentedEntry)
    {
        stream.Seek(0, SeekOrigin.End);
        WriteNewLineIfNeeded(stream, ReadAllBytes(stream));
        if (!string.IsNullOrEmpty(separator))
            WriteUtf8(stream, separator + Environment.NewLine);
        WriteUtf8(stream, indentedEntry + Environment.NewLine);
    }

    /// <summary>
    /// Reads the complete content of a stream into memory.
    /// </summary>
    /// <param name="stream">Opened file stream.</param>
    /// <returns>File bytes.</returns>
    private static byte[] ReadAllBytes(FileStream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var bytes = new byte[stream.Length];
        _ = stream.Read(bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Finds the footer position when it appears as the last non-whitespace token.
    /// </summary>
    /// <param name="bytes">Current file bytes.</param>
    /// <param name="footer">Footer bytes to locate.</param>
    /// <returns>Start index of the footer; otherwise -1.</returns>
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

    /// <summary>
    /// Writes a trailing newline only when the stream does not already end with one.
    /// </summary>
    /// <param name="stream">Opened file stream.</param>
    /// <param name="currentBytes">Current file bytes.</param>
    private static void WriteNewLineIfNeeded(FileStream stream, byte[] currentBytes)
    {
        if (currentBytes.Length == 0)
            return;

        byte last = currentBytes[^1];
        if (last != (byte)'\n' && last != (byte)'\r')
            WriteUtf8(stream, Environment.NewLine);
    }

    /// <summary>
    /// Writes a UTF-8 string to a stream.
    /// </summary>
    /// <param name="stream">Target file stream.</param>
    /// <param name="value">String value to write.</param>
    private static void WriteUtf8(FileStream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Indicates whether a byte is an ASCII whitespace character.
    /// </summary>
    /// <param name="value">Byte value to inspect.</param>
    /// <returns><c>true</c> when the byte is whitespace; otherwise <c>false</c>.</returns>
    private static bool IsAsciiWhitespace(byte value)
        => value is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r';

    /// <summary>
    /// Indents all lines of a multiline block by a fixed amount.
    /// </summary>
    /// <param name="text">Text block to indent.</param>
    /// <param name="spaces">Number of leading spaces per line.</param>
    /// <returns>Indented text block.</returns>
    private static string IndentBlock(string text, int spaces)
    {
        var indent = new string(' ', spaces);
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        for (int i = 0; i < lines.Length; i++)
            lines[i] = indent + lines[i];

        return string.Join(Environment.NewLine, lines);
    }
}
