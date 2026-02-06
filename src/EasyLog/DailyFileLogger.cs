using System.Text;
using EasySave.Configuration;
using EasySave.Log;

namespace EasyLog;

/// <summary>
/// Logger that writes entries to a daily JSON log file with cross-process synchronization.
/// </summary>
public sealed class DailyFileLogger : ILogger, IDisposable
{
    private readonly ILogFormatter _formatter;
    private readonly IPathProvider _pathProvider;
    private readonly string _fileExtension;

    private readonly object _sync = new();

    // Cross-process lock (useful if multiple EasySave processes run).
    private readonly Mutex _mutex;

    public DailyFileLogger(
        ILogFormatter formatter,
        IPathProvider pathProvider,
        string mutexName = "Global\\ProSoft_EasySave_EasyLog_DailyFile",
        string fileExtension = "json")
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _fileExtension = fileExtension;
        _mutex = new Mutex(false, mutexName);
    }

    public void Write(EasySave.Log.LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var normalized = NormalizeEntry(entry);
        var path = _pathProvider.GetDailyLogPath(normalized.Timestamp.Date, _fileExtension);

        EnsureDirectoryExists(path);

        var jsonObject = _formatter.Format(normalized);

        lock (_sync)
        {
            if (!_mutex.WaitOne(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Unable to acquire log file mutex within timeout.");

            try
            {
                AppendEntryToFile(path, jsonObject);
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
        var header = _formatter.GetFileHeader();
        var footer = _formatter.GetFileFooter();
        var separator = _formatter.GetEntrySeparator();
        var indentSpaces = _formatter.GetIndentSpaces();
        var indentedEntry = IndentBlock(formattedEntry, indentSpaces);

        if (!File.Exists(path))
        {
            // Create new file
            var content = new StringBuilder();
            if (!string.IsNullOrEmpty(header))
                content.AppendLine(header);
            content.AppendLine(indentedEntry);
            if (!string.IsNullOrEmpty(footer))
                content.AppendLine(footer);
            
            File.WriteAllText(path, content.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return;
        }

        // Read existing file
        string existingContent = File.ReadAllText(path, Encoding.UTF8);
        
        if (string.IsNullOrWhiteSpace(existingContent))
        {
            // File exists but empty
            var content = new StringBuilder();
            if (!string.IsNullOrEmpty(header))
                content.AppendLine(header);
            content.AppendLine(indentedEntry);
            if (!string.IsNullOrEmpty(footer))
                content.AppendLine(footer);
            
            File.WriteAllText(path, content.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return;
        }

        // Append to existing file
        var newContent = new StringBuilder();
        
        if (!string.IsNullOrEmpty(footer))
        {
            // Remove footer, add entry, add footer back
            int footerIndex = existingContent.LastIndexOf(footer);
            if (footerIndex >= 0)
            {
                var beforeFooter = existingContent.Substring(0, footerIndex).TrimEnd();
                newContent.Append(beforeFooter);
                
                // Check if there's content between header and footer (not just empty array/document)
                bool hasExistingEntries = false;
                if (!string.IsNullOrEmpty(header))
                {
                    var afterHeader = beforeFooter.Substring(header.TrimEnd().Length).Trim();
                    hasExistingEntries = !string.IsNullOrEmpty(afterHeader);
                }
                else
                {
                    hasExistingEntries = !string.IsNullOrEmpty(beforeFooter.Trim());
                }
                
                // Only add separator if there are existing entries
                if (hasExistingEntries && !string.IsNullOrEmpty(separator))
                    newContent.AppendLine(separator);
                else
                    newContent.AppendLine();
                    
                newContent.AppendLine(indentedEntry);
                newContent.AppendLine(footer);
            }
            else
            {
                // Footer not found, just append
                newContent.Append(existingContent.TrimEnd());
                newContent.AppendLine();
                if (!string.IsNullOrEmpty(separator))
                    newContent.AppendLine(separator);
                newContent.AppendLine(indentedEntry);
                if (!string.IsNullOrEmpty(footer))
                    newContent.AppendLine(footer);
            }
        }
        else
        {
            // No footer, just append
            newContent.Append(existingContent.TrimEnd());
            newContent.AppendLine();
            if (!string.IsNullOrEmpty(separator))
                newContent.AppendLine(separator);
            newContent.AppendLine(indentedEntry);
        }
        
        File.WriteAllText(path, newContent.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
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
