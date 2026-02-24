namespace EasySave.Exceptions
{
    public class FileNullException : EasysaveDefaultException
    {
        public FileNullException(
            string path,
            string path_type,
            string details = ""
            )
            : base(
                  Localization.LocalizationKey.error_file_null,
                  [path, path_type],
                  details
            )
        {
        }
        public static void ThrowIfNullOrWhiteSpace(string? path, string pathType)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrEmpty(path))
            {
                throw new FileNullException(path ?? string.Empty, pathType);
            }
        }
    }
}
