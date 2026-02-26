namespace EasySave.Backup;

/// <summary>
/// Provides runtime encryption settings used by backup execution.
/// </summary>
public interface IEncryptionPolicyProvider
{
    /// <summary>
    /// Returns encryption settings to apply for the current run.
    /// </summary>
    /// <returns>Effective encryption policy.</returns>
    EncryptionPolicy GetPolicy();
}
