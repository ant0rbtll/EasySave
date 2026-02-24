namespace EasySave.Exceptions
{
    public class InvalidLogFileException : EasysaveDefaultException
    {
        public InvalidLogFileException(
            string filePath,
            string fileFormat
            ) : base(
                Localization.LocalizationKey.error_invalid_log_file,
                [filePath, fileFormat]
            )
        { }
    }
}
