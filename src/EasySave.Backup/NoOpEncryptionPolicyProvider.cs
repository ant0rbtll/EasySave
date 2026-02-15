namespace EasySave.Backup;

/// <summary>
/// Default policy provider that disables encryption.
/// </summary>
public sealed class NoOpEncryptionPolicyProvider : IEncryptionPolicyProvider
{
    public EncryptionPolicy GetPolicy() => EncryptionPolicy.Disabled;
}
