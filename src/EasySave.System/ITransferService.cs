namespace EasySave.System;

/// <summary>
/// Provides file transfer operations with result tracking.
/// </summary>
public interface ITransferService
{
    /// <summary>
    /// Transfers a file from source to destination.
    /// </summary>
    /// <param name="sourcePath">The source file path.</param>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="overwrite">Whether to overwrite the destination if it exists.</param>
    /// <returns>A <see cref="TransferResult"/> containing transfer details and status.</returns>
    TransferResult TransferFile(string sourcePath, string destinationPath, bool overwrite);
}
