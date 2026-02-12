using EasySave.Persistence;
using EasySave.System;
using System.Diagnostics;

namespace EasySave.Application;

/// <summary>
/// Blocks file-level backup execution when the configured business software is detected.
/// </summary>
public sealed class BusinessSoftwareBackupExecutionGuard(
    IUserPreferencesRepository preferencesRepository,
    Func<string, bool>? isBusinessSoftwareRunning = null) : IBackupExecutionGuard
{
    private static readonly char[] ProcessSeparators = [',', ';'];
    private readonly IUserPreferencesRepository _preferencesRepository = preferencesRepository;
    private readonly Func<string, bool> _isBusinessSoftwareRunning = isBusinessSoftwareRunning ?? IsProcessRunning;

    public void EnsureCanCopyNextFile()
    {
        IReadOnlyList<string> configuredProcessNames = GetConfiguredBusinessSoftwareProcessNames();
        if (configuredProcessNames.Count == 0)
        {
            return;
        }

        foreach (var configuredProcessName in configuredProcessNames)
        {
            if (_isBusinessSoftwareRunning(configuredProcessName))
            {
                throw CreateBusinessSoftwareRunningException(configuredProcessName);
            }
        }
    }

    private IReadOnlyList<string> GetConfiguredBusinessSoftwareProcessNames()
    {
        try
        {
            return ParseConfiguredProcessNames(_preferencesRepository.Load().BusinessSoftwareProcessName);
        }
        catch
        {
            return [];
        }
    }

    private static InvalidOperationException CreateBusinessSoftwareRunningException(string configuredProcessName)
    {
        var exception = new InvalidOperationException("error_business_software_running");
        exception.Data["errorKey"] = "error_business_software_running";
        exception.Data["0"] = configuredProcessName;
        return exception;
    }

    private static string NormalizeProcessName(string? value)
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

    private static IReadOnlyList<string> ParseConfiguredProcessNames(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(ProcessSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeProcessName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsProcessRunning(string processName)
    {
        try
        {
            var candidates = BuildProcessNameCandidates(processName);
            if (candidates.Count == 0)
            {
                return false;
            }

            foreach (var process in Process.GetProcesses())
            {
                string currentName;
                try
                {
                    currentName = process.ProcessName;
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(currentName))
                {
                    continue;
                }

                if (candidates.Contains(currentName))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static HashSet<string> BuildProcessNameCandidates(string processName)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(processName))
        {
            return candidates;
        }

        var trimmed = processName.Trim();
        candidates.Add(trimmed);

        var fileName = Path.GetFileName(trimmed);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            candidates.Add(fileName);
            candidates.Add(Path.GetFileNameWithoutExtension(fileName));
        }

        candidates.RemoveWhere(string.IsNullOrWhiteSpace);
        return candidates;
    }
}
