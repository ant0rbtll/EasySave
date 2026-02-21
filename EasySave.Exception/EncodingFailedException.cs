namespace EasySave.Exceptions
{
    public class EncodingFailedException : EasysaveDefaultException
    {
        public EncodingFailedException(
            string details = ""
            )
            : base(
                  Localization.LocalizationKey.error_encryption_failed,
                  [],
                  details
            )
        {
        }

    }
}
