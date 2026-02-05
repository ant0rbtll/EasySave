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

            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }

            string fileName = $"{date:yyyy-MM-dd}.json";
            string fullPath = Path.Combine(logsDir, fileName);

            if (!File.Exists(fullPath))
            {
                File.WriteAllText(fullPath, string.Empty);
            }

            return fullPath;
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
                return Path.Combine(baseDirectory, "logs");
            }

            var candidate = _logDirectoryOverride.Trim();
            if (Path.IsPathRooted(candidate))
            {
                return candidate;
            }

            return Path.GetFullPath(Path.Combine(baseDirectory, candidate));
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
