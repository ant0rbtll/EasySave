using EasySave.Core;
using EasySave.Localization;

namespace EasySave.UI.Services;

internal interface IConsoleInputService
{
    string? AskString(LocalizationKey key);
    int? AskInt(LocalizationKey key);
    string? AskStringWithCurrentValue(LocalizationKey key, string currentValue);
    int? AskIntWithCurrentValue(LocalizationKey key, int currentValue);
    BackupType? AskBackupType(LocalizationKey key);
    BackupType? AskBackupTypeWithCurrentValue(LocalizationKey key, BackupType currentType);
}
