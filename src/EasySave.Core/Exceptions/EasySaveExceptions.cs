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
    public class FileNullOrNotFoundException : EasysaveDefaultException {
        public FileNullOrNotFoundException(
            string errorKey,
            List<string> details
            )
            : base("",
                  new List<string>(),
                  details.ToString()
            )
        {
        }
    }

    public class DirectoryNullOrNotFoundException : EasysaveDefaultException
    {
        public DirectoryNullOrNotFoundException(
            List<string> details
            )
            : base("error_directory_not_found",
                  new List<string>(),
                  details
            )
        {
        }
    }
}
