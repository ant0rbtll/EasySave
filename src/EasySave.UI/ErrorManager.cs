using EasySave.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.UI
{
    internal class ErrorManager
    {
        private Dictionary<string, LocalizationKey> errors;

        public ErrorManager()
        {
            errors = new Dictionary<string, LocalizationKey>()
            {
                { "error_default", LocalizationKey.error_default },
                { "error_add_max", LocalizationKey.error_add_max },
                { "error_add_exists", LocalizationKey.error_add_exists  },
                { "error_parser_arg_null", LocalizationKey.error_parser_arg_null },
                { "error_parser_arg_invalid", LocalizationKey.error_parser_arg_invalid },
                { "error_file_null", LocalizationKey.error_file_null }

            };
        }

        public LocalizationKey getMessage(string message)
        {
            try { return errors[message]; }
            catch
            {
                return errors["error_default"];
            }
        }
    }
}
