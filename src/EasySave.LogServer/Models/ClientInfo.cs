namespace EasySave.LogServer.Models;

/// <summary>
/// Maps a client MAC address to a human-readable friendly name.
/// </summary>
public record ClientInfo(string MacAddress, string? FriendlyName);
