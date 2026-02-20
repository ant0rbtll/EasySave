namespace EasySave.Core.Exceptions
{
    public class EncodingFailedException : EasysaveDefaultException
    {
        public EncodingFailedException(
            string details = ""
            )
            : base("error_encryption_failed",
                  [],
                  details
            )
        {
        }

    }
    
    public class UnsupportedLogFormatException : EasysaveDefaultException
    {
        public UnsupportedLogFormatException(
            string log_format
            ) : base("error_unspported_log_format",
                [log_format],
                ""
                )
        {

        }
    }

    public class InvalidArgumentException : EasysaveDefaultException
    {
        public InvalidArgumentException(
            string value
            ) : base(
                "error_invalid_argument",
                [value], 
                "")
        {
        }
    }

    public class JobNotFoundException : EasysaveDefaultException
    {
        public JobNotFoundException(int id) : base(
            "error_job_not_found",
            [id.ToString()],
            "")
        {
        }
    }

    public class JobAlreadyExistException : EasysaveDefaultException
    {
        public JobAlreadyExistException(int id) : base(
            "error_job_already_exist",
            [id.ToString()],
            "")
        {
        }
    }
}
