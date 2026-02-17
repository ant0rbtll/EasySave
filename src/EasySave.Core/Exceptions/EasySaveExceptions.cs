namespace EasySave.Core.Exceptions
{
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
