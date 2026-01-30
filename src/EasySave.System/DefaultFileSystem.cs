namespace EasySave.System;

public sealed class DefaultFileSystem : IFileSystem
{
    public bool DirectoryExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

        return Directory.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

        Directory.CreateDirectory(path);
    }

    public bool FileExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

        return File.Exists(path);
    }

    public long GetFileSize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("File not found.", path);

        return new FileInfo(path).Length;
    }

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source path cannot be null or whitespace.", nameof(sourcePath));
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Destination path cannot be null or whitespace.", nameof(destinationPath));

        File.Copy(sourcePath, destinationPath, overwrite);
    }

    public void EnsureDirectoryForFileExists(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));

        var destDir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(destDir) && !Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);
    }

    public IEnumerable<string> EnumerateFilesRecursive(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("Root path cannot be null or whitespace.", nameof(rootPath));
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Directory not found: {rootPath}");

        return Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories);
    }

    public IEnumerable<string> EnumerateDirectoriesRecursive(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("Root path cannot be null or whitespace.", nameof(rootPath));
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Directory not found: {rootPath}");

        return Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories);
    }

    public string Combine(params string[] parts)
    {
        if (parts is null)
            throw new ArgumentNullException(nameof(parts));
        if (parts.Length == 0)
            throw new ArgumentException("Parts cannot be empty.", nameof(parts));
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                throw new ArgumentException("Path parts cannot be null or whitespace.", nameof(parts));
        }

        return Path.Combine(parts);
    }

    public string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

        var p = path.Trim();
        p = p.Replace('\\', Path.DirectorySeparatorChar)
             .Replace('/', Path.DirectorySeparatorChar);

        return p;
    }

    public string GetRelativePath(string rootPath, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("Root path cannot be null or whitespace.", nameof(rootPath));
        if (string.IsNullOrWhiteSpace(fullPath))
            throw new ArgumentException("Full path cannot be null or whitespace.", nameof(fullPath));

        return Path.GetRelativePath(rootPath, fullPath);
    }
}
