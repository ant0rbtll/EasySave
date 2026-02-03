using EasySave.Core;

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
	public void ShowMessage(string key, bool writeLine = true);

	/// <summary>
	/// Display an error translated
	/// </summary>
	/// <param name="key">The translation key</param>
	public void ShowError(string key);

	/// <summary>
	/// Ask the user to type a string
	/// </summary>
	/// <param name="key">The translation key of the question</param>
	/// <returns></returns>
	public string AskString(string key);

    /// <summary>
    /// Ask the user to type an int
    /// </summary>
    /// <param name="key">The translation key of the question</param>
    /// <returns></returns>
    public int AskInt(string key);

    /// <summary>
    /// Ask the user to choose a save type
    /// </summary>
    /// <param name="key">The translation key of the question</param>
    /// <returns></returns>
    public BackupType AskBackupType(string key);

	public void MainMenu();

}
