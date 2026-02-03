using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Core
{
    public class CreateJobEventArgs : EventArgs
    {
        public string Name { get; }
        public string Source { get; }
        public string Destination { get; }
        public BackupType Type { get; }

        public CreateJobEventArgs(string name, string source, string destination, BackupType type)
        {
            Name = name;
            Source = source;
            Destination = destination;
            Type = type;
        }
    }
    public class UpdateJobEventArgs : EventArgs
    {
        public BackupJob Job { get; }

        public UpdateJobEventArgs(BackupJob job)
        {
            Job = job;
        }
    }

    public class AllJobsProvidedEventArgs : EventArgs
    {
        public IReadOnlyList<BackupJob> Jobs { get; }

        public AllJobsProvidedEventArgs(IReadOnlyList<BackupJob> jobs)
        {
            Jobs = jobs;
        }
    }

    public class GetAllJobsRequestedEventArgs : EventArgs
    {
        
    }
}
