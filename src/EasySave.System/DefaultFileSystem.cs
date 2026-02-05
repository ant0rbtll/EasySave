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
            var e = new ArgumentException("error_file_null");
            e.Data["0_path"] = path;
            e.Data["1_type"] = "";
            throw e;
        }
        return Directory.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            var e = new ArgumentException("error_file_null");
            e.Data["0_path"] = path;
            e.Data["1_type"] = "";
            throw e;
        }
        Directory.CreateDirectory(path);
    }

    public bool FileExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            var e = new ArgumentException("error_file_null");
            e.Data["0_path"] = path;
            e.Data["1_type"] = "";
            throw e;
        }
        return File.Exists(path);
    }

    public long GetFileSize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            var e = new ArgumentException("error_file_null");
            e.Data["0_path"] = path;
            e.Data["1_type"] = "";
            throw e;
        }
        if (!File.Exists(path))
        {
            var e = new FileNotFoundException("error_file_not_found");
            e.Data["0_path"] = path;
            e.Data["1_type"] = "";
            throw e;
        }

        return new FileInfo(path).Length;
    }

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            var e = new ArgumentException("error_file_null");
            e.Data["0_path"] = sourcePath;
            e.Data["1_type"] = "Source";
            throw e;
        }
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            var e = new ArgumentException("error_file_null");
            e.Data["0_path"] = destinationPath;
            e.Data["1_type"] = "Destination";
            throw e;
        }

        File.Copy(sourcePath, destinationPath, overwrite);
    }

    public void EnsureDirectoryForFileExists(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            var e = new ArgumentException("error_file_null");
            e.Data["0_path"] = filePath;
            e.Data["1_type"] = "File";
            throw e;
        }

        var destDir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(destDir) && !Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);
    }

    public IEnumerable<string> EnumerateFilesRecursive(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            var e = new ArgumentException("error_file_null");
            e.Data["0_path"] = rootPath;
            e.Data["1_type"] = "Root";
            throw e;
        }
        if (!Directory.Exists(rootPath))
        {
            var e = new DirectoryNotFoundException("error_directory_not_found");
            e.Data["directory"] = rootPath;
            throw e;
        }
        return Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories);
    }

    public IEnumerable<string> EnumerateDirectoriesRecursive(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            var e = new ArgumentException("error_file_null");
            e.Data["0_path"] = rootPath;
            e.Data["1_type"] = "Root";
            throw e;
        }
        if (!Directory.Exists(rootPath))
        {
            var e = new DirectoryNotFoundException("error_directory_not_found");
            e.Data["directory"] = rootPath;
            throw e;
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
            var e = new ArgumentException("error_file_null");
            e.Data["0_path"] = path;
            e.Data["1_type"] = "";
            throw e;
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
            var e = new ArgumentException("error_file_null");
            e.Data["0_path"] = rootPath;
            e.Data["1_type"] = "Root";
            throw e;
        }
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            var e = new ArgumentException("error_file_null");
            e.Data["0_path"] = fullPath;
            e.Data["1_type"] = "Full";
            throw e;
        }

        return Path.GetRelativePath(rootPath, fullPath);
    }
}
