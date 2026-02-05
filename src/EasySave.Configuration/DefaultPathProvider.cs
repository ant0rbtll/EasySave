namespace EasySave.Configuration
{
    public class DefaultPathProvider : IPathProvider
    {
        private readonly string baseDirectory;
        private string? _logDirectoryOverride;

        public DefaultPathProvider()
        {
            baseDirectory = AppContext.BaseDirectory;
        }

        /// <summary>
        /// Creation ou recuperation du dossier de log et du fichier journalier
        /// </summary>
        #region GetDailyLogPath
        public string GetDailyLogPath(DateTime date)
        {
            string logsDir = ResolveLogsDirectory();
            string fileName = $"{date:yyyy-MM-dd}.json";
            string fullPath = Path.Combine(logsDir, fileName);

            try
            {
                EnsureLogFileExists(fullPath);
                return fullPath;
            }
            catch (IOException)
            {
                return TryFallbackToDefault(logsDir, fileName);
            }
            catch (UnauthorizedAccessException)
            {
                return TryFallbackToDefault(logsDir, fileName);
            }
        }
        #endregion

        public void SetLogDirectoryOverride(string? directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                _logDirectoryOverride = null;
                return;
            }

            _logDirectoryOverride = directory.Trim();
        }

        private string ResolveLogsDirectory()
        {
            if (string.IsNullOrWhiteSpace(_logDirectoryOverride))
            {
                return GetDefaultLogsDirectory();
            }

            var candidate = _logDirectoryOverride!;
            if (Path.IsPathRooted(candidate))
            {
                return candidate;
            }

            return Path.GetFullPath(Path.Combine(baseDirectory, candidate));
        }

        private string GetDefaultLogsDirectory()
        {
            return Path.Combine(baseDirectory, "logs");
        }

        private string TryFallbackToDefault(string attemptedLogsDir, string fileName)
        {
            string defaultLogsDir = GetDefaultLogsDirectory();

            if (IsSameDirectory(attemptedLogsDir, defaultLogsDir))
            {
                throw new InvalidOperationException("Unable to create or access the default log directory.");
            }

            string fallbackPath = Path.Combine(defaultLogsDir, fileName);
            EnsureLogFileExists(fallbackPath);
            return fallbackPath;
        }

        private static bool IsSameDirectory(string first, string second)
        {
            string normalizedFirst = NormalizeDirectory(first);
            string normalizedSecond = NormalizeDirectory(second);

            return string.Equals(normalizedFirst, normalizedSecond, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeDirectory(string path)
        {
            string full = Path.GetFullPath(path);
            return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static void EnsureLogFileExists(string fullPath)
        {
            string? dir = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(fullPath))
            {
                File.WriteAllText(fullPath, string.Empty);
            }
        }

        /// <summary>
        /// Creation ou recuperation du fichier d'etat
        /// </summary>
        #region GetStatePath
        public string GetStatePath()
        {
            string path = Path.Combine(baseDirectory, "state.json");

            if (!File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
            }

            return path;
        }
        #endregion

        /// <summary>
        /// Creation ou recuperation du fichier de configuration des jobs
        /// </summary>
        #region GetJobsConfigPath
        public string GetJobsConfigPath()
        {
            string path = Path.Combine(baseDirectory, "jobs.json");

            if (!File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
            }

            return path;
        }
        #endregion

        /// <summary>
        /// Creation ou recuperation du fichier de preferences utilisateur
        /// </summary>
        #region GetUserPreferencesPath
        public string GetUserPreferencesPath()
        {
            string path = Path.Combine(baseDirectory, "user-preferences.json");

            if (!File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
            }

            return path;
        }
        #endregion
    }
}
