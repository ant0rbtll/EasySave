namespace EasySave.Exceptions
{
    public class UnsupportedLogFormatException : EasysaveDefaultException
    {
        public UnsupportedLogFormatException(
            string log_format
            ) : base(
                Localization.LocalizationKey.error_unsupported_log_format,
                [log_format],
                ""
                )
        {

        }
    }
}
