using EasySave.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.UI
{
    /// <summary>
    /// Maps error message keys to localization keys for user-facing error display.
    /// </summary>
    internal class ErrorManager
    {
        private readonly Dictionary<string, LocalizationKey> errors;

        public ErrorManager()
        {
            errors = new Dictionary<string, LocalizationKey>()
            {
                { "error_default", LocalizationKey.error_default },
                { "error_add_max", LocalizationKey.error_add_max },
                { "error_add_exists", LocalizationKey.error_add_exists  },
                { "error_parser_arg_null", LocalizationKey.error_parser_arg_null },
                { "error_parser_arg_invalid", LocalizationKey.error_parser_arg_invalid },
                { "error_file_null", LocalizationKey.error_file_null },
                { "error_job_not_found", LocalizationKey.error_job_not_found },
                { "error_max_id", LocalizationKey.error_max_id },
                { "error_file_transfer_failed", LocalizationKey.error_file_transfer_failed },
                { "error_backup_type_invalid", LocalizationKey.error_backup_type_invalid },
                { "error_file_not_found", LocalizationKey.error_file_not_found },
                { "error_directory_not_found", LocalizationKey.error_directory_not_found },
                { "error_parts_empty", LocalizationKey.error_parts_empty },
                { "error_parts_null", LocalizationKey.error_parts_null }
            };
        }

        public bool TryGetMessage(string message, out LocalizationKey key)
        {
            return errors.TryGetValue(message, out key);
        }
    }
}
