using EasySave.State.Configuration.Paths;
using System;

namespace EasySave.Configuration.Paths
{
    public class DefaultPathProvider : IPathProvider
    {
        private readonly string baseDirectory;

        public DefaultPathProvider()
        {
            // Emplacement robuste et accepté sur les serveurs
            baseDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave"
            );

            Directory.CreateDirectory(baseDirectory);
        }

        public string GetDailyLogPath(DateTime date)
        {
            string fileName = $"{date:yyyy-MM-dd}.json";
            return Path.Combine(baseDirectory, "logs", fileName);
        }

        public string GetStatePath()
        {
            return Path.Combine(baseDirectory, "state.json");
        }

        public string GetJobsConfigPath()
        {
            return Path.Combine(baseDirectory, "jobs.json");
        }
    }
}
