namespace EasySave.UI;

public interface IUI
{
	public void showMessage(string key);

	public void showError(string key);

	public string askString(string key);

	public int askInt(string key);

	/**
	 * ask the user about the type of the backup
	 * 
	 * @return : 
	 */
	public string askBackupType(string key);
}
