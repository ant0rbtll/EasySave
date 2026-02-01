using EasySave.Core;

namespace EasySave.UI;

/// <summary>
/// The Use interface of the app
/// </summary>
public interface IUI
{
	/// <summary>
	/// Display a message translated
	/// </summary>
	/// <param name="key">The translation key</param>
	public void showMessage(string key, bool writeLine);

	/// <summary>
	/// Display an error translated
	/// </summary>
	/// <param name="key">The translation key</param>
	public void showError(string key);

	/// <summary>
	/// Ask the user to type a string
	/// </summary>
	/// <param name="key">The translation key of the question</param>
	/// <returns></returns>
	public string askString(string key);

    /// <summary>
    /// Ask the user to type an int
    /// </summary>
    /// <param name="key">The translation key of the question</param>
    /// <returns></returns>
    public int askInt(string key);

    /// <summary>
    /// Ask the user to choose a save type
    /// </summary>
    /// <param name="key">The translation key of the question</param>
    /// <returns></returns>
    public BackupType askBackupType(string key);

}
