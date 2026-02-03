using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Core
{
    public class EventService  : IEventService
    {
        public event EventHandler<CreateJobEventArgs> OnCreateJob;
        public event EventHandler<UpdateJobEventArgs> OnUpdateJob;
        public event EventHandler<int> OnDeleteJob;
        public event EventHandler<int> OnRunJob;

        public void RaiseCreateJob(CreateJobEventArgs args) => OnCreateJob?.Invoke(this, args);
        public void RaiseUpdateJob(UpdateJobEventArgs args) => OnUpdateJob?.Invoke(this, args);
        public void RaiseDeleteJob(int id) => OnDeleteJob?.Invoke(this, id);
        public void RaiseRunJob(int id) => OnRunJob?.Invoke(this, id);
    }
}
