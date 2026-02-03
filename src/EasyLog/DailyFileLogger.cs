using System.Text;
using EasyLog.Configuration;

namespace EasyLog;

public sealed class DailyFileLogger : ILogger, IDisposable
{
    private readonly ILogFormatter _formatter;
    private readonly IPathProvider _pathProvider;

    private readonly object _sync = new();

    // Cross-process lock (useful if multiple EasySave processes run).
    private readonly Mutex _mutex;

    public DailyFileLogger(
        ILogFormatter formatter,
        IPathProvider pathProvider,
        string mutexName = "Global\\ProSoft_EasySave_EasyLog_DailyFile")
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _mutex = new Mutex(false, mutexName);
    }

    public void Write(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var normalized = NormalizeEntry(entry);
        var path = _pathProvider.GetDailyLogPath(normalized.Timestamp.Date);

        EnsureDirectoryExists(path);

        var jsonObject = _formatter.Format(normalized);

        lock (_sync)
        {
            if (!_mutex.WaitOne(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Unable to acquire log file mutex within timeout.");

            try
            {
                AppendJsonObjectToArrayFile(path, jsonObject);
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

    private static LogEntry NormalizeEntry(LogEntry e)
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

    private static void AppendJsonObjectToArrayFile(string path, string jsonObject)
    {
        if (!File.Exists(path))
        {
            using var fsNew = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var writerNew = new StreamWriter(fsNew, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            writerNew.WriteLine("[");
            writerNew.WriteLine(IndentBlock(jsonObject, 2));
            writerNew.WriteLine("]");
            writerNew.Flush();
            return;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

        if (stream.Length == 0)
        {
            using var writerEmpty = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
            writerEmpty.WriteLine("[");
            writerEmpty.WriteLine(IndentBlock(jsonObject, 2));
            writerEmpty.WriteLine("]");
            writerEmpty.Flush();
            return;
        }

        long endPos = stream.Length - 1;
        int lastByte;

        while (true)
        {
            if (endPos < 0)
                throw new InvalidOperationException("Daily log file is empty or corrupted.");

            stream.Position = endPos;
            lastByte = stream.ReadByte();
            if (lastByte < 0)
                throw new InvalidOperationException("Unable to read daily log file.");

            if (!char.IsWhiteSpace((char)lastByte))
                break;

            endPos--;
        }

        if (lastByte != ']')
            throw new InvalidOperationException("Daily log file is not a JSON array (missing closing bracket).");

        long prevPos = endPos - 1;
        int prevByte = -1;

        while (prevPos >= 0)
        {
            stream.Position = prevPos;
            prevByte = stream.ReadByte();
            if (prevByte < 0)
                break;

            if (!char.IsWhiteSpace((char)prevByte))
                break;

            prevPos--;
        }

        bool isEmptyArray = prevByte == '[';

        stream.Position = endPos;

        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);

        if (!isEmptyArray)
            writer.WriteLine(",");

        writer.WriteLine(IndentBlock(jsonObject, 2));
        writer.WriteLine("]");
        writer.Flush();

        stream.SetLength(stream.Position);
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
