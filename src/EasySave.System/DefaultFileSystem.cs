using EasySave.Exceptions;
using System;
using System.IO;

namespace EasySave.System;

/// <summary>
/// Default implementation of <see cref="IFileSystem"/> using the standard .NET file system APIs.
/// </summary>
public sealed class DefaultFileSystem : IFileSystem
{
    public bool DirectoryExists(string path)
    {
        FileNullException.ThrowIfNullOrWhiteSpace(path, "");

        return Directory.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        FileNullException.ThrowIfNullOrWhiteSpace(path, "");

        Directory.CreateDirectory(path);
    }

    public bool FileExists(string path)
    {
        FileNullException.ThrowIfNullOrWhiteSpace(path, "");

        return File.Exists(path);
    }

    public long GetFileSize(string path)
    {
        FileNullException.ThrowIfNullOrWhiteSpace(path, "");

        if (!File.Exists(path))
        {
            throw new FileNullException(path, "", "File not found");
        }

        return new FileInfo(path).Length;
    }

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
    {
        FileNullException.ThrowIfNullOrWhiteSpace(sourcePath, "Source");
        FileNullException.ThrowIfNullOrWhiteSpace(destinationPath, "Destination");

        File.Copy(sourcePath, destinationPath, overwrite);
    }

    public void EnsureDirectoryForFileExists(string filePath)
    {
        FileNullException.ThrowIfNullOrWhiteSpace(filePath, "File");

        var destDir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(destDir) && !Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);
    }

    public IEnumerable<string> EnumerateFilesRecursive(string rootPath, IEnumerable<string>? priorityExtensions = null)
    {
        FileNullException.ThrowIfNullOrWhiteSpace(rootPath, "Root");

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNullOrNotFoundException(rootPath);
        }
        var allFiles = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories).ToList();

        if (priorityExtensions == null || !priorityExtensions.Any())
        {
            return allFiles;
        }

        var prioritySet = new HashSet<string>(priorityExtensions, StringComparer.OrdinalIgnoreCase);
       
        return allFiles
            .OrderByDescending(f => prioritySet.Contains(Path.GetExtension(f)))
            .ThenBy(f => f);
        //return Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories);
    }

    public IEnumerable<string> EnumerateDirectoriesRecursive(string rootPath)
    {
        FileNullException.ThrowIfNullOrWhiteSpace(rootPath, "Root");

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNullOrNotFoundException(rootPath);
        }

        return Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories);
    }

    public string Combine(params string[] parts)
    {
        if (parts is null)
            throw new InvalidArgumentException(nameof(parts));
        if (parts.Length == 0)
            throw new EasysaveDefaultException(Localization.LocalizationKey.error_parts_empty, []);
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                throw new EasysaveDefaultException(Localization.LocalizationKey.error_parts_null, []);
        }

        return Path.Combine(parts);
    }

    public string NormalizePath(string path)
    {
        FileNullException.ThrowIfNullOrWhiteSpace(path, "");
        var p = path.Trim();
        p = p.Replace('\\', Path.DirectorySeparatorChar)
             .Replace('/', Path.DirectorySeparatorChar);

        return p;
    }

    public string GetRelativePath(string rootPath, string fullPath)
    {
        FileNullException.ThrowIfNullOrWhiteSpace(rootPath, "Root");
        FileNullException.ThrowIfNullOrWhiteSpace(fullPath, "Full");

        return Path.GetRelativePath(rootPath, fullPath);
    }
}
