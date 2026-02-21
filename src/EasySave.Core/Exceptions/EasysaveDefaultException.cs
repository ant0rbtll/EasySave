using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Core.Exceptions
{
    public class EasysaveDefaultException : SystemException
    {
        protected string _errorKey = string.Empty;
        protected List<string> _options;
        protected string _details = "";


        public EasysaveDefaultException(string errorKey, List<string> options, string details = "")
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
