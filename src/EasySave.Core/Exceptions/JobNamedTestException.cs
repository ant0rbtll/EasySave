using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Core.Exceptions
{
    public class JobNamedTestException : EasysaveDefaultException
    {
        public JobNamedTestException(
            string[] options
            ) 
            : base("Le job doit pas s'appeller {0}",
                  "job_name_test",
                  200,
                  options)
        {

        }
    }
}
