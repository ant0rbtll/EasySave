using System.Text;
using EasySave.Configuration;
using EasySave.Core;
using EasySave.Log;

namespace EasyLog;

/// <summary>
/// Logger that writes entries to a daily JSON log file with cross-process synchronization.
/// </summary>
public sealed class DailyFileLogger : ILogger, IDisposable
{
    private readonly ILogFormatter _formatter;
    private readonly IPathProvider _pathProvider;
    private readonly LogFormat _format;

    private readonly object _sync = new();

    // Cross-process lock (useful if multiple EasySave processes run).
    private readonly Mutex _mutex;

    public DailyFileLogger(
        ILogFormatter formatter,
        IPathProvider pathProvider,
        string mutexName = "Global\\ProSoft_EasySave_EasyLog_DailyFile",
        LogFormat format = LogFormat.Json)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _format = format;
        _mutex = new Mutex(false, mutexName);
    }

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

    public void Dispose()
    {
        _mutex.Dispose();
    }

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

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var p = path.Trim();
        p = p.Replace('/', '\\');
        return p;
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(dir))
            return;

        Directory.CreateDirectory(dir);
    }

    private void AppendEntryToFile(string path, string formattedEntry)
    {
        var indentSpaces = _formatter.GetIndentSpaces();
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

    private void CreateNewLogFile(string path, string indentedEntry)
    {
        var header = _formatter.GetFileHeader();
        var footer = _formatter.GetFileFooter();

        var content = new StringBuilder();
        if (!string.IsNullOrEmpty(header))
            content.AppendLine(header);
        content.AppendLine(indentedEntry);
        if (!string.IsNullOrEmpty(footer))
            content.AppendLine(footer);

        File.WriteAllText(path, content.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private void AppendToExistingFile(FileStream stream, string indentedEntry)
    {
        var footer = _formatter.GetFileFooter();
        var separator = _formatter.GetEntrySeparator();

        if (string.IsNullOrEmpty(footer))
        {
            AppendWithoutFooter(stream, separator, indentedEntry);
            return;
        }

        AppendBeforeFooter(stream, footer, separator, indentedEntry);
    }

    private void AppendBeforeFooter(FileStream stream, string footer, string separator, string indentedEntry)
    {
        var footerBytes = Encoding.UTF8.GetBytes(footer);
        var bytes = ReadAllBytes(stream);
        int footerIndex = LastIndexOf(bytes, footerBytes);

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

    private bool CheckIfHasExistingEntries(string beforeFooter)
    {
        var header = _formatter.GetFileHeader();

        if (!string.IsNullOrEmpty(header))
        {
            var afterHeader = beforeFooter.Substring(header.TrimEnd().Length).Trim();
            return !string.IsNullOrEmpty(afterHeader);
        }

        return !string.IsNullOrEmpty(beforeFooter.Trim());
    }

    private static void AppendWithoutFooter(FileStream stream, string separator, string indentedEntry)
    {
        stream.Seek(0, SeekOrigin.End);
        WriteNewLineIfNeeded(stream, ReadAllBytes(stream));
        if (!string.IsNullOrEmpty(separator))
            WriteUtf8(stream, separator + Environment.NewLine);
        WriteUtf8(stream, indentedEntry + Environment.NewLine);
    }

    private static byte[] ReadAllBytes(FileStream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var bytes = new byte[stream.Length];
        _ = stream.Read(bytes, 0, bytes.Length);
        return bytes;
    }

    private static int LastIndexOf(byte[] bytes, byte[] pattern)
    {
        if (pattern.Length == 0 || bytes.Length < pattern.Length)
            return -1;

        for (int i = bytes.Length - pattern.Length; i >= 0; i--)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (bytes[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
                return i;
        }

        return -1;
    }

    private static void WriteNewLineIfNeeded(FileStream stream, byte[] currentBytes)
    {
        if (currentBytes.Length == 0)
            return;

        byte last = currentBytes[^1];
        if (last != (byte)'\n' && last != (byte)'\r')
            WriteUtf8(stream, Environment.NewLine);
    }

    private static void WriteUtf8(FileStream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string IndentBlock(string text, int spaces)
    {
        var indent = new string(' ', spaces);
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        for (int i = 0; i < lines.Length; i++)
            lines[i] = indent + lines[i];

        return string.Join(Environment.NewLine, lines);
    }
}
