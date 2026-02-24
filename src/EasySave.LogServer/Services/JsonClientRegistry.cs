using System.Text.Json;
using EasySave.Exceptions;
using EasySave.LogServer.Models;

namespace EasySave.LogServer.Services;

/// <summary>
/// JSON file-backed client registry for MAC-to-friendly-name mapping.
/// Thread-safe via ReaderWriterLockSlim.
/// </summary>
public sealed class JsonClientRegistry : IClientRegistry, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;
    private readonly ReaderWriterLockSlim _lock = new();
    private Dictionary<string, string?> _clients;

    public JsonClientRegistry(string filePath)
    {
        EasysaveDefaultException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        _clients = LoadFromDisk();
    }

    public void EnsureRegistered(string macAddress, string? hostname = null)
    {
        var normalized = NormalizeMac(macAddress);

        _lock.EnterUpgradeableReadLock();
        try
        {
            var exists = _clients.TryGetValue(normalized, out var currentName);
            if (exists && !string.IsNullOrWhiteSpace(currentName))
                return;

            _lock.EnterWriteLock();
            try
            {
                _clients[normalized] = string.IsNullOrWhiteSpace(hostname) ? null : hostname;
                SaveToDisk();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    public string? GetFriendlyName(string macAddress)
    {
        var normalized = NormalizeMac(macAddress);

        _lock.EnterReadLock();
        try
        {
            return _clients.TryGetValue(normalized, out var name) ? name : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void SetFriendlyName(string macAddress, string friendlyName)
    {
        var normalized = NormalizeMac(macAddress);

        _lock.EnterWriteLock();
        try
        {
            _clients[normalized] = friendlyName;
            SaveToDisk();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IReadOnlyList<ClientInfo> GetAll()
    {
        _lock.EnterReadLock();
        try
        {
            return _clients
                .Select(kv => new ClientInfo(kv.Key, kv.Value))
                .OrderBy(c => c.MacAddress)
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }

    private Dictionary<string, string?> LoadFromDisk()
    {
        if (!File.Exists(_filePath))
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var json = File.ReadAllText(_filePath);
            var list = JsonSerializer.Deserialize<List<ClientInfo>>(json, JsonOptions);
            if (list is null)
                return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            return list.ToDictionary(
                c => NormalizeMac(c.MacAddress),
                c => c.FriendlyName,
                StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveToDisk()
    {
        try
        {
            var list = _clients
                .Select(kv => new ClientInfo(kv.Key, kv.Value))
                .OrderBy(c => c.MacAddress)
                .ToList();

            var json = JsonSerializer.Serialize(list, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to save client registry to {_filePath}: {ex.Message}");
        }
    }

    private static string NormalizeMac(string macAddress)
    {
        return (macAddress ?? string.Empty).Trim().ToUpperInvariant();
    }
}
