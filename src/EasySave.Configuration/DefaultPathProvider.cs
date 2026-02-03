namespace EasySave.Configuration
{
    public class DefaultPathProvider : IPathProvider
    {
        private readonly string baseDirectory;

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
            string logsDir = Path.Combine(baseDirectory, "logs");

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
    }
}