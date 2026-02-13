using EasySave.Core;
using EasySave.Localization;

namespace EasySave.UI.Services;

/// <summary>
/// Defines interactive input prompts used by console flows.
/// </summary>
public interface IConsoleInputService
{
    /// <summary>
    /// Prompts the user for a non-empty string value.
    /// </summary>
    /// <param name="key">Localization key of the prompt.</param>
    /// <returns>The entered value, or <see langword="null"/> if cancelled.</returns>
    string? AskString(LocalizationKey key);

    /// <summary>
    /// Prompts the user for an integer value.
    /// </summary>
    /// <param name="key">Localization key of the prompt.</param>
    /// <returns>The entered value, or <see langword="null"/> if cancelled.</returns>
    int? AskInt(LocalizationKey key);

    /// <summary>
    /// Prompts for a string value while allowing the current value to be kept.
    /// </summary>
    /// <param name="key">Localization key of the prompt.</param>
    /// <param name="currentValue">Current field value.</param>
    /// <returns>The updated value, or <see langword="null"/> if cancelled.</returns>
    string? AskStringWithCurrentValue(LocalizationKey key, string currentValue);

    /// <summary>
    /// Prompts for an integer value while allowing the current value to be kept.
    /// </summary>
    /// <param name="key">Localization key of the prompt.</param>
    /// <param name="currentValue">Current field value.</param>
    /// <returns>The updated value, or <see langword="null"/> if cancelled.</returns>
    int? AskIntWithCurrentValue(LocalizationKey key, int currentValue);

    /// <summary>
    /// Prompts for a backup type selection.
    /// </summary>
    /// <param name="key">Localization key of the prompt.</param>
    /// <returns>The selected backup type, or <see langword="null"/> if cancelled.</returns>
    BackupType? AskBackupType(LocalizationKey key);

    /// <summary>
    /// Prompts for a backup type while allowing the current value to be kept.
    /// </summary>
    /// <param name="key">Localization key of the prompt.</param>
    /// <param name="currentType">Current backup type.</param>
    /// <returns>The selected backup type, or <see langword="null"/> if cancelled.</returns>
    BackupType? AskBackupTypeWithCurrentValue(LocalizationKey key, BackupType currentType);
}
