namespace EasySave.Exceptions
{
    public class InvalidArgumentException : EasysaveDefaultException
    {
        public InvalidArgumentException(
            string value,
            string details = ""
            ) : base(
                Localization.LocalizationKey.error_invalid_argument,
                [value],
                details)
        {
        }
    }
}
