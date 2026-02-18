using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Core;
using EasySave.Log;

namespace EasySave.AppCommon.Services;

/// <summary>
/// Logger implementation that sends log entries to a centralized log server via HTTP POST.
/// </summary>
public sealed class HttpLogSender : ILogger, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly string _macAddress;
    private readonly LogFormat _logFormat;

    public HttpLogSender(string serverUrl, LogFormat logFormat)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUrl);
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(serverUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        _macAddress = ResolveMacAddress();
        _logFormat = logFormat;
    }

    internal HttpLogSender(HttpMessageHandler handler, string serverUrl, LogFormat logFormat)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUrl);
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(serverUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        _macAddress = ResolveMacAddress();
        _logFormat = logFormat;
    }

    public void Write(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var request = new
        {
            entry.Timestamp,
            entry.BackupId,
            entry.BackupName,
            EventType = entry.EventType.ToString(),
            entry.SourcePathUNC,
            entry.DestinationPathUNC,
            entry.FileSizeBytes,
            entry.TransferTimeMs,
            entry.EncryptionTimeMs,
            MacAddress = _macAddress,
            Hostname = Environment.MachineName,
            LogFormat = _logFormat.ToString()
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            _httpClient.PostAsync("api/logs", content)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to send log to centralized server.", ex);
        }
    }

    /// <summary>
    /// Performs a lightweight health check against the server (GET /api/config).
    /// Throws on failure.
    /// </summary>
    public void CheckServerHealth()
    {
        _httpClient.GetAsync("api/config")
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult()
            .EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    /// <summary>
    /// Retrieves the MAC address of the first operational, non-loopback network interface.
    /// Prefers Ethernet and Wi-Fi interfaces over virtual adapters.
    /// </summary>
    private static string ResolveMacAddress()
    {
        try
        {
            var preferredTypes = new[]
            {
                NetworkInterfaceType.Ethernet,
                NetworkInterfaceType.Wireless80211
            };

            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                            && !IsVirtualInterface(n))
                .OrderBy(n => Array.IndexOf(preferredTypes, n.NetworkInterfaceType) is var idx && idx >= 0 ? idx : int.MaxValue)
                .ThenBy(n => n.Name);

            var nic = interfaces.FirstOrDefault();
            if (nic is not null)
            {
                var bytes = nic.GetPhysicalAddress().GetAddressBytes();
                if (bytes.Length > 0)
                {
                    return string.Join(":", bytes.Select(b => b.ToString("X2")));
                }
            }
        }
        catch
        {
            // Fallback if network interfaces cannot be enumerated.
        }

        return "00:00:00:00:00:00";
    }

    private static bool IsVirtualInterface(NetworkInterface nic)
    {
        var name = nic.Name;
        return name.StartsWith("br-", StringComparison.Ordinal)
            || name.StartsWith("veth", StringComparison.Ordinal)
            || name.StartsWith("virbr", StringComparison.Ordinal)
            || name.StartsWith("docker", StringComparison.Ordinal)
            || name.StartsWith("gpd", StringComparison.Ordinal);
    }
}
