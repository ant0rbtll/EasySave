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
            string value,
            string details = ""
            ) : base(
                "error_invalid_argument",
                [value], 
                details)
        {
        }
    }
    public class FileNullOrNotFoundException : EasysaveDefaultException {
        public FileNullOrNotFoundException(
            string path,
            string path_type,
            string details = ""
            )
            : base("error_file_null",
                  [path, path_type],
                  details
            )
        {
        }
        public static void ThrowIfNullOrWhiteSpace(string? path, string pathType)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrEmpty(path))
            {
                throw new FileNullOrNotFoundException(path, pathType);
            }
        }
    }

    public class DirectoryNullOrNotFoundException : EasysaveDefaultException
    {
        public DirectoryNullOrNotFoundException(
            List<string> details
            )
            : base("error_directory_not_found",
                  new List<string>(),
                  details.ToString()
            )
        {
        }
    }
}
