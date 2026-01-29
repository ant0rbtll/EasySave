namespace EasySave.System;

public sealed class DefaultFileSystem : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public bool FileExists(string path) => File.Exists(path);

    public long GetFileSize(string path) => new FileInfo(path).Length;

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
        => File.Copy(sourcePath, destinationPath, overwrite);

    public IEnumerable<string> EnumerateFilesRecursive(string rootPath)
        => Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories);

    public IEnumerable<string> EnumerateDirectoriesRecursive(string rootPath)
        => Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories);

    public string Combine(params string[] parts) => Path.Combine(parts);

    public string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var p = path.Trim();
        p = p.Replace('\\', Path.DirectorySeparatorChar)
             .Replace('/', Path.DirectorySeparatorChar);

        return p;
    }

    public string GetRelativePath(string rootPath, string fullPath)
        => Path.GetRelativePath(rootPath, fullPath);
}