using EasySave.Core;
using EasySave.Core.Exceptions;
using EasySave.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Application.Readers
{
    public abstract class LogReaderBase : ILogReader
    {
        protected const long MaxLogFileSizeBytes = 50L * 1024 * 1024; // 50 MB

        public abstract LogFormat Format { get; }

        public IReadOnlyList<LogEntry> ReadEntries(string filePath)
        {
            FileNullOrNotFoundException.ThrowIfNullOrWhiteSpace(filePath, "");

            if (!File.Exists(filePath))
            {
                throw new FileNullOrNotFoundException(filePath, "", "Log file not found.");
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
