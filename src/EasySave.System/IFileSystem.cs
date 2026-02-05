namespace EasySave.System;

/// <summary>
/// Abstracts file system operations for testability.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Checks whether a directory exists at the specified path.
    /// </summary>
    /// <param name="path">The directory path to check.</param>
    /// <returns>True if the directory exists, otherwise false.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Creates a directory at the specified path.
    /// </summary>
    /// <param name="path">The directory path to create.</param>
    void CreateDirectory(string path);

    /// <summary>
    /// Checks whether a file exists at the specified path.
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns>True if the file exists, otherwise false.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Gets the size of the specified file in bytes.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The file size in bytes.</returns>
    long GetFileSize(string path);

    /// <summary>
    /// Copies a file from source to destination.
    /// </summary>
    /// <param name="sourcePath">The source file path.</param>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="overwrite">Whether to overwrite the destination if it exists.</param>
    void CopyFile(string sourcePath, string destinationPath, bool overwrite);

    /// <summary>
    /// Ensures the parent directory of a file exists, creating it if necessary.
    /// </summary>
    /// <param name="filePath">The file path whose parent directory should exist.</param>
    void EnsureDirectoryForFileExists(string filePath);

    /// <summary>
    /// Enumerates all files recursively under the specified directory.
    /// </summary>
    /// <param name="rootPath">The root directory to search.</param>
    /// <returns>An enumerable of file paths.</returns>
    IEnumerable<string> EnumerateFilesRecursive(string rootPath);

    /// <summary>
    /// Enumerates all subdirectories recursively under the specified directory.
    /// </summary>
    /// <param name="rootPath">The root directory to search.</param>
    /// <returns>An enumerable of directory paths.</returns>
    IEnumerable<string> EnumerateDirectoriesRecursive(string rootPath);

    /// <summary>
    /// Combines path segments into a single path.
    /// </summary>
    /// <param name="parts">The path segments to combine.</param>
    /// <returns>The combined path.</returns>
    string Combine(params string[] parts);

    /// <summary>
    /// Normalizes a path by replacing alternate separators with the platform separator.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    string NormalizePath(string path);

    /// <summary>
    /// Gets the relative path from a root directory to a full path.
    /// </summary>
    /// <param name="rootPath">The root directory path.</param>
    /// <param name="fullPath">The full file path.</param>
    /// <returns>The relative path.</returns>
    string GetRelativePath(string rootPath, string fullPath);
}
