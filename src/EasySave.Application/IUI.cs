using EasySave.Core;
using EasySave.Localization;

namespace EasySave.Application;

/// <summary>
/// The Use interface of the app
/// </summary>
public interface IUI
{
	/// <summary>
	/// Display a message translated
	/// </summary>
	/// <param name="key">The translation key</param>
	public void ShowMessage(LocalizationKey key, bool writeLine = true);
    public void ShowMessage(string message, bool writeLine = true);

	/// <summary>
	/// Display an error translated
	/// </summary>
	/// <param name="key">The translation key</param>
	public void ShowError(LocalizationKey key);

	/// <summary>
	/// Ask the user to type a string
	/// </summary>
	/// <param name="key">The translation key of the question</param>
	/// <returns></returns>
	public string AskString(LocalizationKey key);

    /// <summary>
    /// Ask the user to type an int
    /// </summary>
    /// <param name="key">The translation key of the question</param>
    /// <returns></returns>
    public int AskInt(LocalizationKey key);

    /// <summary>
    /// Ask the user to choose a save type
    /// </summary>
    /// <param name="key">The translation key of the question</param>
    /// <returns></returns>
    public BackupType AskBackupType(LocalizationKey key);

}
