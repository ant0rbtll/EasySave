using EasySave.State.Configuration.Paths;
using System;
using System.IO;


namespace EasySave.Configuration.Paths
{
    public class DefaultPathProvider : IPathProvider
    {
        private readonly string baseDirectory;

        public DefaultPathProvider()
        {
            // Dossier d'exécution de l'application
            baseDirectory = AppContext.BaseDirectory;
        }

        // Logs journaliers
        public string GetDailyLogPath(DateTime date)
        {
            string logsDir = Path.Combine(baseDirectory, "logs");

            // Crée le dossier logs s'il n'existe pas
            Directory.CreateDirectory(logsDir);

            // Nom du fichier log
            string fileName = $"{date:yyyy-MM-dd}.json";
            string fullPath = Path.Combine(logsDir, fileName);

            // Crée le fichier s'il n'existe pas
            if (!File.Exists(fullPath))
            {
                using (File.Create(fullPath)) { } // crée et ferme immédiatement
            }

            return fullPath;
        }

        // Fichier state.json
        public string GetStatePath()
        {
            string path = Path.Combine(baseDirectory, "state.json");

            if (!File.Exists(path))
            {
                using (File.Create(path)) { }
            }

            return path;
        }

        // Fichier jobs.json
        public string GetJobsConfigPath()
        {
            string path = Path.Combine(baseDirectory, "jobs.json");

            if (!File.Exists(path))
            {
                using (File.Create(path)) { }
            }

            return path;
        }
    }
}