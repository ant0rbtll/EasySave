using EasySave.Core;
using System.Text.Json.Serialization;

namespace EasySave.Persistence;

/// <summary>
/// Represents user preferences and settings.
/// </summary>
public class UserPreferences
{
    private static readonly char[] BusinessSoftwareSeparators = [',', ';'];
    private List<string>? _encryptedExtensions;
    private List<string>? _businessSoftwareProcessNames;
    private string _encryptionProvider = EncryptionProviderNames.DotNet;
    private long _parallelLargeFileThresholdValue;

    /// <summary>
    /// Gets or sets the application theme preference.
    /// </summary>
    public ThemePreference ThemePreference { get; set; } = ThemePreference.System;

    /// <summary>
    /// Gets or sets the application language/culture.
    /// </summary>
    public string Language { get; set; } = "fr";

    /// <summary>
    /// Gets or sets the custom log directory path (optional).
    /// </summary>
    public string? LogDirectory { get; set; }

    /// <summary>
    /// Gets or sets the log output format.
    /// </summary>
    public LogFormat LogFormat { get; set; } = LogFormat.Json;

    /// <summary>
    /// Gets or sets the log delivery mode (local, centralized, or both).
    /// </summary>
    public LogMode LogMode { get; set; } = LogMode.Local;

    /// <summary>
    /// Gets or sets the URL of the centralized log server.
    /// Required when LogMode is Centralized or LocalAndCentralized.
    /// </summary>
    public string? LogServerUrl { get; set; }

    /// <summary>
    /// Gets or sets file extensions that should be encrypted after transfer.
    /// </summary>
    public List<string> EncryptedExtensions
    {
        get => _encryptedExtensions ??= [];
        set => _encryptedExtensions = value ?? [];
    }

    /// <summary>
    /// Gets or sets selected encryption provider name.
    /// </summary>
    public string EncryptionProvider
    {
        get => string.IsNullOrWhiteSpace(_encryptionProvider) ? EncryptionProviderNames.DotNet : _encryptionProvider;
        set => _encryptionProvider = string.IsNullOrWhiteSpace(value) ? EncryptionProviderNames.DotNet : value;
    }

    /// <summary>
    /// Gets or sets optional external CryptoSoft executable path.
    /// </summary>
    public string? CryptoSoftExecutablePath { get; set; }

    /// <summary>
    /// Gets or sets business software process names to monitor.
    /// Backup launch is blocked when this process is running.
    /// </summary>
    public List<string> BusinessSoftwareProcessNames
    {
        get => _businessSoftwareProcessNames ??= [];
        set => _businessSoftwareProcessNames = NormalizeBusinessSoftwareProcessNames(value);
    }

    /// <summary>
    /// Backward compatibility with legacy single-string persisted field.
    /// </summary>
    [JsonPropertyName("businessSoftwareProcessName")]
    public string? LegacyBusinessSoftwareProcessName
    {
        set => BusinessSoftwareProcessNames = ParseLegacyBusinessSoftwareProcessNames(value);
    }

    private List<string>? _priorityExtensions;

    /// <summary>
    /// Gets or sets file extensions that are considered priority.
    /// Non-priority files wait for these to be finished across all jobs.
    /// </summary>
    public List<string> PriorityExtensions
    {
        get => _priorityExtensions ??= [];
        set => _priorityExtensions = value ?? [];
    }

    /// <summary>
    /// Gets or sets the threshold value for large-file parallel transfer limiting.
    /// Value less than or equal to zero disables the limit.
    /// </summary>
    public long ParallelLargeFileThresholdValue
    {
        get => _parallelLargeFileThresholdValue;
        set => _parallelLargeFileThresholdValue = value < 0 ? 0 : value;
    }

    /// <summary>
    /// Gets or sets the unit used by <see cref="ParallelLargeFileThresholdValue"/>.
    /// </summary>
    public TransferSizeUnit ParallelLargeFileThresholdUnit { get; set; } = TransferSizeUnit.Kilo;

    /// <summary>
    /// Converts the configured large-file threshold to bytes.
    /// Returns 0 when the feature is disabled or when conversion overflows.
    /// </summary>
    public long GetParallelLargeFileThresholdBytes()
    {
        if (ParallelLargeFileThresholdValue <= 0)
        {
            return 0;
        }

        try
        {
            checked
            {
                var factor = ParallelLargeFileThresholdUnit switch
                {
                    TransferSizeUnit.Giga => 1024L * 1024L * 1024L,
                    TransferSizeUnit.Mega => 1024L * 1024L,
                    _ => 1024L
                };

                return ParallelLargeFileThresholdValue * factor;
            }
        }
        catch (OverflowException)
        {
            return long.MaxValue;
        }
    }

    private static List<string> NormalizeBusinessSoftwareProcessNames(IEnumerable<string>? names)
    {
        if (names is null)
        {
            return [];
        }

        return names
            .Select(NormalizeBusinessSoftwareProcessName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> ParseLegacyBusinessSoftwareProcessNames(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(BusinessSoftwareSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeBusinessSoftwareProcessName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeBusinessSoftwareProcessName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var fileName = Path.GetFileName(trimmed);
        var withoutExtension = Path.GetFileNameWithoutExtension(fileName);
        return string.IsNullOrWhiteSpace(withoutExtension) ? trimmed : withoutExtension;
    }
}
