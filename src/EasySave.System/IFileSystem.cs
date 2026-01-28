namespace EasySave.System;

public interface IFileSystem
{
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    IEnumerable<string> EnumerateFiles(string path);
    IEnumerable<string> EnumerateDirectories(string path);
    void CopyFile(string src, string dst);
    long GetFileSize(string path);
    string GetFullPath(string path);
}
