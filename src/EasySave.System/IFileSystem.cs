namespace EasySave.System;

public interface IFileSystem
{
    bool DirectoryExists(string path);
    void CreateDirectory(string path);

    bool FileExists(string path);
    long GetFileSize(string path);

    void CopyFile(string sourcePath, string destinationPath, bool overwrite);

    IEnumerable<string> EnumerateFilesRecursive(string rootPath);
    IEnumerable<string> EnumerateDirectoriesRecursive(string rootPath);

    string Combine(params string[] parts);
    string NormalizePath(string path);
    string GetRelativePath(string rootPath, string fullPath);
}