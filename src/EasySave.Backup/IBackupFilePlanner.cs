using EasySave.Core;
using EasySave.Exceptions;
using EasySave.System;

namespace EasySave.Backup;

/// <summary>
/// Builds a file execution plan for one backup job.
/// </summary>
public interface IBackupFilePlanner
{
    /// <summary>
    /// Builds ordered file plans including priority and copy decision.
    /// </summary>
    IReadOnlyList<BackupFilePlan> BuildPlans(BackupJob job, IEnumerable<string>? priorityExtensions);
}

/// <summary>
/// Default implementation based on the configured file system.
/// </summary>
public sealed class DefaultBackupFilePlanner(IFileSystem fileSystem) : IBackupFilePlanner
{
    private readonly IFileSystem _fileSystem = fileSystem;

    public IReadOnlyList<BackupFilePlan> BuildPlans(BackupJob job, IEnumerable<string>? priorityExtensions)
    {
        EasysaveDefaultException.ThrowIfNull(job);

        var normalizedExtensions = NormalizePriorityExtensions(priorityExtensions);
        var priorityExtensionSet = new HashSet<string>(normalizedExtensions, StringComparer.OrdinalIgnoreCase);
        var files = _fileSystem.EnumerateFilesRecursive(job.Source, normalizedExtensions).ToList();

        var plans = new List<BackupFilePlan>(files.Count);
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(job.Source, file);
            var destinationFile = Path.Combine(job.Destination, relativePath);
            var sourceFileSizeBytes = _fileSystem.GetFileSize(file);
            var normalizedExtension = NormalizeExtension(Path.GetExtension(file));
            var isPriority = normalizedExtension is not null && priorityExtensionSet.Contains(normalizedExtension);
            var shouldCopy = ShouldCopyFile(job.Type, file, destinationFile, sourceFileSizeBytes);
            plans.Add(new BackupFilePlan(file, destinationFile, sourceFileSizeBytes, isPriority, shouldCopy));
        }

        return plans;
    }

    private bool ShouldCopyFile(BackupType type, string sourceFile, string destinationFile, long sourceFileSizeBytes)
    {
        if (type == BackupType.Complete)
        {
            return true;
        }

        if (type == BackupType.Differential)
        {
            var destinationDir = Path.GetDirectoryName(destinationFile)!;
            if (!_fileSystem.DirectoryExists(destinationDir))
            {
                return true;
            }

            if (!_fileSystem.FileExists(destinationFile))
            {
                return true;
            }

            var destSize = _fileSystem.GetFileSize(destinationFile);
            return sourceFileSizeBytes != destSize;
        }

        throw new EasysaveDefaultException(Localization.LocalizationKey.error_backup_type_invalid, [type.ToString()]);
    }

    private static List<string> NormalizePriorityExtensions(IEnumerable<string>? extensions)
    {
        if (extensions is null)
        {
            return [];
        }

        return extensions
            .Select(NormalizeExtension)
            .Where(e => e is not null)
            .Select(e => e!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        var normalized = extension.Trim();
        if (!normalized.StartsWith(".", StringComparison.Ordinal))
        {
            normalized = "." + normalized;
        }

        return normalized.ToLowerInvariant();
    }
}
