using EasySave.Localization;

namespace EasySave.Exceptions
{
    public class EasysaveDefaultException : SystemException, ITranslatableException
    {
        public LocalizationKey ErrorKey { get; }
        public List<string> Options { get; }
        public string Details { get; } = "";


        public EasysaveDefaultException(LocalizationKey errorKey, List<string> options, string details = "")
        {
            ErrorKey = errorKey;
            Options = options;
            Details = details;
        }

        public static void ThrowIfNullOrWhiteSpace(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrEmpty(value))
            {
                throw new InvalidArgumentException(nameof(value));
            }
        }

        public static void ThrowIfNull(object? argument)
        {
            if (argument is null)
            {
                throw new InvalidArgumentException(nameof(argument));
            }
        }
    }
}
