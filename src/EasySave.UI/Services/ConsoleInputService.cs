using EasySave.Core;
using EasySave.Localization;

namespace EasySave.UI.Services;

/// <summary>
/// Handles interactive user prompts in the console.
/// </summary>
internal class ConsoleInputService(
    IConsoleMessageService messageService,
    IConsoleAdapter consoleAdapter) : IConsoleInputService
{
    private readonly IConsoleMessageService _messageService = messageService;
    private readonly IConsoleAdapter _consoleAdapter = consoleAdapter;

    public string? AskString(LocalizationKey key)
    {
        _messageService.Write(key, false);
        _messageService.Write(LocalizationKey.input_escape_to_cancel, false);
        _consoleAdapter.Write(" : ");

        string input = string.Empty;
        ConsoleKeyInfo keyInfo;

        do
        {
            keyInfo = _consoleAdapter.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                _consoleAdapter.WriteLine();
                return null;
            }

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                _consoleAdapter.WriteLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    _messageService.Write(LocalizationKey.input_string_invalid, false);
                    _messageService.Write(LocalizationKey.input_escape_to_cancel, false);
                    _consoleAdapter.Write(" : ");
                    input = string.Empty;
                    continue;
                }

                return input;
            }

            if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input = input[..^1];
                _consoleAdapter.Write("\b \b");
                continue;
            }

            if (!char.IsControl(keyInfo.KeyChar))
            {
                input += keyInfo.KeyChar;
                _consoleAdapter.Write(keyInfo.KeyChar.ToString());
            }
        }
        while (true);
    }

    public int? AskInt(LocalizationKey key)
    {
        _messageService.Write(key, false);
        _messageService.Write(LocalizationKey.input_escape_to_cancel, false);
        _consoleAdapter.Write(" : ");

        string input = string.Empty;
        ConsoleKeyInfo keyInfo;

        do
        {
            keyInfo = _consoleAdapter.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                _consoleAdapter.WriteLine();
                return null;
            }

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                _consoleAdapter.WriteLine();
                if (int.TryParse(input, out var numberInput))
                {
                    return numberInput;
                }

                _messageService.Write(LocalizationKey.input_number_invalid, false);
                _messageService.Write(LocalizationKey.input_escape_to_cancel, false);
                _consoleAdapter.Write(" : ");
                input = string.Empty;
                continue;
            }

            if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input = input[..^1];
                _consoleAdapter.Write("\b \b");
                continue;
            }

            if (char.IsDigit(keyInfo.KeyChar) || (keyInfo.KeyChar == '-' && input.Length == 0))
            {
                input += keyInfo.KeyChar;
                _consoleAdapter.Write(keyInfo.KeyChar.ToString());
            }
        }
        while (true);
    }

    public string? AskStringWithCurrentValue(LocalizationKey key, string currentValue)
    {
        _messageService.Write(key, false);
        _messageService.Write(LocalizationKey.input_escape_to_cancel, false);
        _messageService.WriteWithParams(LocalizationKey.input_enter_to_keep_current, [currentValue], false);
        _consoleAdapter.Write(" : ");

        string input = string.Empty;
        ConsoleKeyInfo keyInfo;

        do
        {
            keyInfo = _consoleAdapter.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                _consoleAdapter.WriteLine();
                return null;
            }

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                _consoleAdapter.WriteLine();
                if (input.Length == 0)
                {
                    return currentValue;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    _messageService.Write(LocalizationKey.input_string_invalid, false);
                    _messageService.Write(LocalizationKey.input_escape_to_cancel, false);
                    _messageService.WriteWithParams(LocalizationKey.input_enter_to_keep_current, [currentValue], false);
                    _consoleAdapter.Write(" : ");
                    input = string.Empty;
                    continue;
                }

                return input;
            }

            if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input = input[..^1];
                _consoleAdapter.Write("\b \b");
                continue;
            }

            if (!char.IsControl(keyInfo.KeyChar))
            {
                input += keyInfo.KeyChar;
                _consoleAdapter.Write(keyInfo.KeyChar.ToString());
            }
        }
        while (true);
    }

    public int? AskIntWithCurrentValue(LocalizationKey key, int currentValue)
    {
        _messageService.Write(key, false);
        _messageService.Write(LocalizationKey.input_escape_to_cancel, false);
        _messageService.WriteWithParams(LocalizationKey.input_enter_to_keep_current, [currentValue.ToString()], false);
        _consoleAdapter.Write(" : ");

        string input = string.Empty;
        ConsoleKeyInfo keyInfo;

        do
        {
            keyInfo = _consoleAdapter.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                _consoleAdapter.WriteLine();
                return null;
            }

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                _consoleAdapter.WriteLine();
                if (input.Length == 0)
                {
                    return currentValue;
                }

                if (int.TryParse(input, out var numberInput))
                {
                    return numberInput;
                }

                _messageService.Write(LocalizationKey.input_number_invalid, false);
                _messageService.Write(LocalizationKey.input_escape_to_cancel, false);
                _messageService.WriteWithParams(LocalizationKey.input_enter_to_keep_current, [currentValue.ToString()], false);
                _consoleAdapter.Write(" : ");
                input = string.Empty;
                continue;
            }

            if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input = input[..^1];
                _consoleAdapter.Write("\b \b");
                continue;
            }

            if (char.IsDigit(keyInfo.KeyChar) || (keyInfo.KeyChar == '-' && input.Length == 0))
            {
                input += keyInfo.KeyChar;
                _consoleAdapter.Write(keyInfo.KeyChar.ToString());
            }
        }
        while (true);
    }

    public BackupType? AskBackupType(LocalizationKey key)
    {
        _messageService.Write(LocalizationKey.backupjob_type_list);
        var values = Enum.GetValues(typeof(BackupType)).Cast<BackupType>().ToArray();

        for (var index = 0; index < values.Length; index++)
        {
            _consoleAdapter.WriteLine($"{index + 1}. {values[index]}");
        }

        _messageService.Write(key);

        while (true)
        {
            var backupTypeInput = AskInt(LocalizationKey.user_choice);
            if (backupTypeInput == null)
            {
                return null;
            }

            var choice = backupTypeInput.Value;
            if (choice >= 1 && choice <= values.Length)
            {
                return values[choice - 1];
            }

            _messageService.Write(LocalizationKey.input_backuptype_invalid);
        }
    }

    public BackupType? AskBackupTypeWithCurrentValue(LocalizationKey key, BackupType currentType)
    {
        _messageService.Write(LocalizationKey.backupjob_type_list);
        var values = Enum.GetValues(typeof(BackupType)).Cast<BackupType>().ToArray();

        for (var index = 0; index < values.Length; index++)
        {
            _consoleAdapter.WriteLine($"{index + 1}. {values[index]}");
        }

        _messageService.Write(key);
        var currentChoice = Array.IndexOf(values, currentType) + 1;

        while (true)
        {
            var backupTypeInput = AskIntWithCurrentValue(LocalizationKey.user_choice, currentChoice);
            if (backupTypeInput == null)
            {
                return null;
            }

            var choice = backupTypeInput.Value;
            if (choice >= 1 && choice <= values.Length)
            {
                return values[choice - 1];
            }

            _messageService.Write(LocalizationKey.input_backuptype_invalid);
        }
    }
}
