namespace EasySave.Core;

/// <summary>
/// Defines the available log delivery modes.
/// </summary>
public enum LogMode
{
    /// <summary>
    /// Logs are written to local daily files only (default, backward-compatible).
    /// </summary>
    Local,

    /// <summary>
    /// Logs are sent to the centralized server only (no local file).
    /// </summary>
    Centralized,

    /// <summary>
    /// Logs are written both locally and sent to the centralized server.
    /// </summary>
    LocalAndCentralized
}
