using EasySave.LogServer.Models;

namespace EasySave.LogServer.Services;

/// <summary>
/// Registry that maps client MAC addresses to friendly names.
/// </summary>
public interface IClientRegistry
{
    /// <summary>
    /// Ensures a MAC address is registered. Adds it with a null friendly name if unknown.
    /// </summary>
    void EnsureRegistered(string macAddress);

    /// <summary>
    /// Gets the friendly name for a MAC address, or null if not set.
    /// </summary>
    string? GetFriendlyName(string macAddress);

    /// <summary>
    /// Sets the friendly name for a MAC address.
    /// </summary>
    void SetFriendlyName(string macAddress, string friendlyName);

    /// <summary>
    /// Gets all registered clients.
    /// </summary>
    IReadOnlyList<ClientInfo> GetAll();
}
