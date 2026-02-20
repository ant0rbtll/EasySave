using EasySave.Core.Exceptions;
using System.IO;
namespace EasySave.System;

/// <summary>
/// Default implementation of <see cref="IFileSystem"/> using the standard .NET file system APIs.
/// </summary>
public sealed class DefaultFileSystem : IFileSystem
{
    public bool DirectoryExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new FileNullOrNotFoundException("error_file_null", [path.ToString(), ""]);
        }
        return Directory.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new FileNullOrNotFoundException("error_file_null", [path.ToString(), ""]);
        }
        Directory.CreateDirectory(path);
    }

    public bool FileExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new FileNullOrNotFoundException("error_file_null", [path.ToString(), ""]);
        }
        return File.Exists(path);
    }

    public long GetFileSize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new FileNullOrNotFoundException("error_file_null", [path.ToString(), ""]);
        }
        if (!File.Exists(path))
        {
            throw new FileNullOrNotFoundException("error_file_not_found", [path.ToString(), ""]);
        }

        return new FileInfo(path).Length;
    }

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new FileNullOrNotFoundException("error_file_null", [sourcePath.ToString(), "Source"]);
        }
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new FileNullOrNotFoundException("error_file_null", [destinationPath.ToString(), "Destination"]);
        }

        File.Copy(sourcePath, destinationPath, overwrite);
    }

    public void EnsureDirectoryForFileExists(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
           throw new FileNullOrNotFoundException("error_file_null", [filePath.ToString(), "File"]);
        }

        var destDir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(destDir) && !Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);
    }

    public IEnumerable<string> EnumerateFilesRecursive(string rootPath, IEnumerable<string>? priorityExtensions = null)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new FileNullOrNotFoundException("error_file_null", [rootPath.ToString(), "Root"]);
        }
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNullOrNotFoundException([rootPath]);
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
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new FileNullOrNotFoundException("error_file_null", [rootPath.ToString(), "Root"]);
        }
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNullOrNotFoundException([rootPath]);
        }

        return Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories);
    }

    public string Combine(params string[] parts)
    {
        if (parts is null)
            throw new ArgumentNullException(nameof(parts));
        if (parts.Length == 0)
            throw new ArgumentException("error_parts_empty");
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                throw new ArgumentException("error_parts_null");
        }

        return Path.Combine(parts);
    }

    public string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new FileNullOrNotFoundException("error_file_null", [path.ToString(), ""]);
        }
        var p = path.Trim();
        p = p.Replace('\\', Path.DirectorySeparatorChar)
             .Replace('/', Path.DirectorySeparatorChar);

        return p;
    }

    public string GetRelativePath(string rootPath, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new FileNullOrNotFoundException("error_file_null", [rootPath.ToString(), "Root"]);
        }
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            throw new FileNullOrNotFoundException("error_file_null", [fullPath.ToString(), "Full"]);
        }

        return Path.GetRelativePath(rootPath, fullPath);
    }
}
