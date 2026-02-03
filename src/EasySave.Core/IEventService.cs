using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Core
{
    public interface IEventService
    {
        // Événements de création
        event EventHandler<CreateJobEventArgs> OnCreateJob;
        void RaiseCreateJob(CreateJobEventArgs args);

        // Événements de mise à jour
        event EventHandler<UpdateJobEventArgs> OnUpdateJob;
        void RaiseUpdateJob(UpdateJobEventArgs args);

        // Événements de suppression
        event EventHandler<int> OnDeleteJob;
        void RaiseDeleteJob(int id);

        // Événements d'exécution
        event EventHandler<int> OnRunJob;
        void RaiseRunJob(int id);
    }
}
