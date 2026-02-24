namespace EasySave.Exceptions
{   
    public class DirectoryNullOrNotFoundException : EasysaveDefaultException
    {
        public DirectoryNullOrNotFoundException(
            string directoryPath,
            string details = ""
            )
            : base(
                  Localization.LocalizationKey.error_directory_not_found,
                  [directoryPath],
                  details
            )
        {
        }
    }
}
