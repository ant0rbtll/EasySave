namespace EasySave.Log;

/// <summary>
/// Defines the types of events that can be logged during a backup operation.
/// </summary>
public enum LogEventType
{
    /// <summary>
    /// A directory was created at the destination.
    /// </summary>
    CreateDirectory = 0,

    /// <summary>
    /// A file was transferred from source to destination.
    /// </summary>
    TransferFile = 1,

    /// <summary>
    /// An error occurred during the backup operation.
    /// </summary>
    Error = 2,

    /// <summary>
    ///  The backup operation was started.
    /// </summary>
    StartBackup = 3,

    /// <summary>
    /// The backup operation was completed successfully.
    /// </summary>
    EndBackup = 4
}
