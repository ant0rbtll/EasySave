using EasySave.Core;
using EasySave.Exceptions;
using EasySave.Log;

namespace EasySave.Application.Readers
{
    public abstract class LogReaderBase : ILogReader
    {
        protected const long MaxLogFileSizeBytes = 50L * 1024 * 1024; // 50 MB

        public abstract LogFormat Format { get; }

        public IReadOnlyList<LogEntry> ReadEntries(string filePath)
        {
            FileNullException.ThrowIfNullOrWhiteSpace(filePath, "");

            if (!File.Exists(filePath))
            {
                throw new FileNullException(filePath, "", "Log file not found.");
            }

            string log = FileReadResilience.ReadAllTextWithRetry(filePath, MaxLogFileSizeBytes);
            if (string.IsNullOrWhiteSpace(log))
            {
                return [];
            }
            return GetEntries(log, filePath);
        }

        protected abstract IReadOnlyList<LogEntry> GetEntries(string log, string filePath);
    }
}
