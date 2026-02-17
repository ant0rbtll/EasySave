namespace EasySave.Core.Exceptions
{
    public class JobNamedTestException : EasysaveDefaultException
    {
        public JobNamedTestException(
            List<string> options
            )
            : base("job_name_test",
                  options)
        {
        }
    }
    public class EncodingFailedException : EasysaveDefaultException
    { 
        public EncodingFailedException(
            string details = ""
            )
            : base("error_encryption_failed",
                  new List<string>(),
                  details
            )
        {
        }
    }
}
